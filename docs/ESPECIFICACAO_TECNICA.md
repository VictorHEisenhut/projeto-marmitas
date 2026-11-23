# Especificação Técnica e Arquitetura — Plataforma de Marmitas

Documento técnico consolidado contemplando arquitetura, modelagem de dados, máquinas de estado, fluxos operacionais, segurança, LGPD, testes, infraestrutura e decisões técnicas do projeto.

---

## 1. Diagramas de Arquitetura e Fluxos (Mermaid)

### 1.1. Arquitetura Geral

```mermaid
flowchart TB
    subgraph ClientLayer["Camada de Apresentação (Web / PWA)"]
        WebCustomer["Área Pública & Cliente"]
        WebPartner["Painel do Restaurante"]
        WebCourier["Painel do Entregador"]
        WebAdmin["Painel Administrativo"]
    end

    subgraph ApiLayer["ASP.NET Core Web API (.NET 8)"]
        Middleware["ExceptionHandling & RateLimiter & Auth JWT"]
        Controllers["Controllers REST (/api e /api/v1)"]
        SignalRHub["SignalR Hub (/hubs/tracking)"]
        BgWorkers["PlatformBackgroundWorker (Jobs Agendados)"]
    end

    subgraph AppDomain["Camadas de Aplicação e Domínio"]
        Services["Application Services & Use Cases"]
        Ledger["FinancialLedgerService & DistributionService"]
        Domain["Entities, State Machines, ValueObjects & Events"]
    end

    subgraph InfraLayer["Infraestrutura e Persistência"]
        EfCore["EF Core / AppDbContext"]
        Cache["Cache Service (Memory / Redis)"]
        EventBus["Event Bus (Channels / RabbitMQ)"]
        Gateways["Payment Gateways (MercadoPago, Stripe, PagarMe, Asaas)"]
        Geo["Serviço de CEP & Geocoding"]
    end

    subgraph DataLayer["Armazenamento"]
        Postgres[("PostgreSQL")]
        Redis[("Redis Cache")]
    end

    ClientLayer -->|HTTPS REST| Middleware
    ClientLayer <-->|WebSockets| SignalRHub
    Middleware --> Controllers
    Controllers --> Services
    Controllers --> Ledger
    BgWorkers --> Services
    Services --> Domain
    Services --> EfCore
    Services --> Cache
    Services --> EventBus
    Services --> Gateways
    Services --> Geo
    EfCore --> Postgres
    Cache --> Redis
```

### 1.2. Diagrama de Componentes do Monólito Modular

```mermaid
flowchart LR
    Identity["Módulo Identity & RBAC"]
    Users["Módulo Users & LGPD"]
    Restaurants["Módulo Restaurants & Menu"]
    Orders["Módulo Orders"]
    Payments["Módulo Payments"]
    Subscriptions["Módulo Subscriptions & Plans"]
    Delivery["Módulo Delivery & Drivers"]
    Financial["Módulo Financial & Ledger"]
    Notifications["Módulo Notifications"]
    Admin["Módulo Admin & Audit"]

    Orders --> Restaurants
    Orders --> Subscriptions
    Orders --> Payments
    Orders --> Delivery
    Payments --> Financial
    Delivery --> Financial
    Orders --> Notifications
    Subscriptions --> Notifications
    Admin --> Financial
```

### 1.3. Fluxo do Pedido (Venda Avulsa)

```mermaid
sequenceDiagram
    participant C as Cliente
    participant API as Orders / Payments API
    participant R as Restaurante
    participant E as Entregador
    participant L as Ledger Financeiro

    C->>API: POST /api/v1/pedidos (Idempotency-Key)
    API-->>C: 201 Created (Status: Created, ConfirmationPin)
    C->>API: POST /api/v1/pedidos/{id}/pagamento
    API->>L: Registra Receita, Comissão (10%), Taxa Gateway e Líquido
    API-->>C: 201 Created (Status: Paid, PIX / Cartão aprovado)
    R->>API: POST /api/v1/pedidos/{id}/historico-status (Confirmed -> Preparing)
    R->>API: POST /api/v1/pedidos/{id}/historico-status (ReadyForPickup)
    E->>API: PATCH /api/v1/entregas/{id}/aceitar (DriverAssigned)
    E->>API: PATCH /api/v1/entregas/{id}/status (PickedUp -> InTransit)
    E->>API: PATCH /api/v1/entregas/{id}/status (Delivered + ConfirmationPin)
    API->>L: Registra Crédito da Taxa de Entrega ao Entregador
    API-->>C: Notificação em tempo real (Pedido Entregue)
```

### 1.4. Fluxo de Pagamento e Webhook

```mermaid
sequenceDiagram
    participant C as Cliente
    participant P as PaymentsController
    participant G as IPaymentGateway
    participant DB as PostgreSQL (Payments & Ledger)

    C->>P: POST /api/v1/pedidos/{id}/pagamento
    P->>DB: Verifica Idempotency-Key em idempotency_records
    P->>G: ChargeAsync(OrderId, Amount, Method)
    G-->>P: PaymentGatewayResult (Approved, ExternalId, PixQrCode)
    P->>DB: Salva Payment, PaymentTransaction e FinancialTransactions
    P-->>C: 201 Created
    G->>P: POST /api/v1/pagamentos/webhook (X-Webhook-Signature)
    P->>G: ValidateWebhookSignature(payload, signature)
    P->>DB: Atualiza status e registra transação
    P-->>G: 200 OK
```

### 1.5. Fluxo de Assinatura e Resgate de Refeições

```mermaid
sequenceDiagram
    participant C as Cliente
    participant S as SubscriptionsController
    participant O as OrdersController
    participant W as PlatformBackgroundWorker

    C->>S: POST /api/v1/assinaturas (PlanId)
    S-->>C: 201 Created (Active, TotalMealsInCycle)
    C->>O: POST /api/v1/pedidos (IsPlanRedemption = true)
    O->>O: Valida assinatura ativa, limite semanal e incrementa UsedMealsInCycle
    O-->>C: 201 Created (Valor base R$ 0,00 + extras)
    W->>S: Job verifica NextBillingDate <= UtcNow
    W->>S: Renova ciclo, reseta UsedMealsInCycle e gera cobrança
```

### 1.6. Fluxo de Entrega e Rastreamento GPS

```mermaid
sequenceDiagram
    participant R as Restaurante
    participant D as DeliveriesController
    participant E as Entregador
    participant H as OrderTrackingHub (SignalR)
    participant C as Cliente

    R->>D: Marca pedido como ReadyForPickup
    E->>D: GET /api/v1/entregas (Valida CPF, CNH, Veículo e End. Cobrança)
    E->>D: PATCH /api/v1/entregas/{id}/aceitar
    loop Em rota de entrega
        E->>D: PATCH /api/v1/entregadores/me/localizacao (Lat, Lon)
        D->>H: NotifyDriverLocationAsync
        H-->>C: Evento DriverLocationUpdated
    end
    E->>D: PATCH /api/v1/entregas/{id}/status (Delivered + PIN)
    D->>H: NotifyOrderStatusChangedAsync
```

### 1.7. Fluxo de Autenticação, Refresh Token e RBAC

```mermaid
sequenceDiagram
    participant U as Usuário
    participant A as AuthController
    participant DB as PostgreSQL

    U->>A: POST /api/v1/auth/login (Email, Password, MfaCode)
    A->>DB: Verifica Lockout, Hash BCrypt e MFA
    A->>DB: Emite JWT (Claims: sub, role, permission) + Salva RefreshToken
    A-->>U: 200 OK (AccessToken, RefreshToken, ExpiresAt)
    U->>A: POST /api/v1/auth/refresh-token (RefreshToken)
    A->>DB: Revoga RefreshToken atual e emite novo par (Rotation)
    A-->>U: 200 OK (Novo AccessToken + Novo RefreshToken)
```

### 1.8. Comunicação Assíncrona e Jobs

```mermaid
flowchart LR
    Action["Ação na API"] --> DomainEvent["Domain Event"]
    DomainEvent --> EventBus["InMemoryEventBus / RabbitMQ"]
    EventBus --> Notifier["Notificações In-App & SignalR"]
    EventBus --> Audit["Gravação em audit_logs"]
    Timer["PeriodicTimer (5 min)"] --> Worker["PlatformBackgroundWorker"]
    Worker --> Renew["Renovação de Assinaturas"]
    Worker --> Cleanup["Limpeza de Tokens e Idempotência"]
```

### 1.9. Diagrama Entidade-Relacionamento (DER)

```mermaid
erDiagram
    USERS ||--o{ USER_ROLES : possui
    ROLES ||--o{ USER_ROLES : atribui
    ROLES ||--o{ ROLE_PERMISSIONS : contem
    PERMISSIONS ||--o{ ROLE_PERMISSIONS : vincula
    USERS ||--o{ ADDRESSES : cadastra
    USERS ||--o{ USER_CONSENTS : aceita
    USERS ||--o{ REFRESH_TOKENS : mantem
    USERS ||--o{ SUBSCRIPTIONS : contrata
    PLANS ||--o{ SUBSCRIPTIONS : define
    SUBSCRIPTIONS ||--o{ SUBSCRIPTION_DELIVERIES : agenda
    RESTAURANTS ||--o{ RESTAURANT_USERS : emprega
    RESTAURANTS ||--o{ RESTAURANT_HOURS : funciona
    RESTAURANTS ||--o{ DELIVERY_ZONES : atende
    RESTAURANTS ||--o{ PRODUCTS : oferece
    PRODUCT_CATEGORIES ||--o{ PRODUCTS : classifica
    PRODUCTS ||--o{ PRODUCT_OPTIONS : possui
    USERS ||--o{ ORDERS : realiza
    RESTAURANTS ||--o{ ORDERS : recebe
    ORDERS ||--|{ ORDER_ITEMS : contem
    ORDERS ||--o{ ORDER_STATUS_HISTORIES : registra
    ORDERS ||--o| PAYMENTS : paga
    PAYMENTS ||--o{ PAYMENT_TRANSACTIONS : processa
    PAYMENTS ||--o{ REFUNDS : estorna
    ORDERS ||--o| DELIVERIES : gera
    COURIERS ||--o{ COURIER_DOCUMENTS : comprova
    COURIERS ||--o{ COURIER_VEHICLES : utiliza
    COURIERS ||--o{ DRIVER_LOCATIONS : reporta
    COURIERS ||--o{ DELIVERIES : executa
    ORDERS ||--o{ COMMISSIONS : gera
    ORDERS ||--o{ FINANCIAL_TRANSACTIONS : contabiliza
```

### 1.10. Diagrama de Deployment e Infraestrutura

```mermaid
flowchart TB
    subgraph Edge["Camada de Borda"]
        CDN["CDN / DNS / HTTPS"]
        LB["Load Balancer / Reverse Proxy"]
    end

    subgraph Compute["Containers / Azure Web App"]
        ApiNode1["Marmitas.Api (Instância 1)"]
        ApiNode2["Marmitas.Api (Instância 2)"]
    end

    subgraph BackingServices["Serviços Gerenciados"]
        PG[("PostgreSQL + PostGIS")]
        RD[("Redis Cache & Locks")]
        MQ[["RabbitMQ / Service Bus"]]
    end

    CDN --> LB
    LB --> ApiNode1
    LB --> ApiNode2
    ApiNode1 --> PG
    ApiNode2 --> PG
    ApiNode1 --> RD
    ApiNode2 --> RD
    ApiNode1 --> MQ
    ApiNode2 --> MQ
```

---

## 2. Máquinas de Estado

### 2.1. Pedido (`OrderStatus`)

```mermaid
stateDiagram-v2
    [*] --> Created
    Created --> PaymentPending
    Created --> Paid
    Created --> Cancelled
    PaymentPending --> Paid
    PaymentPending --> Failed
    PaymentPending --> Cancelled
    Paid --> Confirmed
    Paid --> Preparing
    Paid --> Cancelled
    Paid --> Refunded
    Confirmed --> Preparing
    Confirmed --> Cancelled
    Preparing --> ReadyForPickup
    ReadyForPickup --> DriverAssigned
    DriverAssigned --> PickedUp
    PickedUp --> InTransit
    InTransit --> Delivered
    Delivered --> Refunded
    Cancelled --> Refunded
```

### 2.2. Pagamento (`Payment.Status`)
* `Pending` $\rightarrow$ `Authorized` $\rightarrow$ `Approved` $\rightarrow$ `Refunded` / `Chargeback`
* `Pending` $\rightarrow$ `Declined` / `Failed`

### 2.3. Assinatura (`Subscription.Status`)
* `PendingPayment` $\rightarrow$ `Active` $\leftrightarrow$ `Paused`
* `Active` $\rightarrow$ `GracePeriod` $\rightarrow$ `Suspended` / `Expired`
* `Active` / `Paused` / `GracePeriod` $\rightarrow$ `Cancelled`

### 2.4. Entrega (`Delivery.Status`)
* `ReadyForPickup` $\rightarrow$ `DriverAssigned` $\rightarrow$ `PickedUp` $\rightarrow$ `InTransit` $\rightarrow$ `Delivered` / `Cancelled`

---

## 3. Critérios de Aceite Principais

* **Cadastro e Login**:
  * *Dado* um visitante com dados válidos, *Quando* enviar `POST /api/v1/auth/registro`, *Então* a conta é criada com perfil `customer`, o consentimento LGPD é registrado e o status `201 Created` é retornado.
  * *Dado* um usuário com 5 tentativas consecutivas de senha incorreta, *Quando* tentar novo login, *Então* a conta fica temporariamente bloqueada por 15 minutos contra força bruta.
* **Pedido e Máquina de Estados**:
  * *Dado* um pedido no status `Delivered`, *Quando* houver tentativa de alterar o status para `Preparing` ou cancelar pelo cliente, *Então* o sistema rejeita a transição com `409 Conflict` (`ORDER_INVALID_STATUS_TRANSITION`).
* **Assinatura**:
  * *Dado* um cliente com assinatura ativa, *Quando* resgatar marmitas acima do limite semanal do plano, *Então* o pedido é bloqueado informando o saldo restante da semana.
* **Entrega**:
  * *Dado* um entregador logado, *Quando* não tiver preenchido CPF, CNH, Veículo e Endereço de Cobrança, *Então* a fila de entregas permanece bloqueada e o aceite de corridas é impedido.
  * *Dado* uma entrega já aceita pelo Entregador A, *Quando* o Entregador B tentar aceitar a mesma entrega, *Então* o sistema retorna `409 Conflict`.
* **Pagamento e Reembolso**:
  * *Dado* um pagamento aprovado, *Quando* o administrador acionar `POST /api/v1/pagamentos/{id}/estornar`, *Então* o pagamento e o pedido mudam para `Refunded` e um lançamento compensatório de débito é gravado no Ledger financeiro sem apagar o lançamento original.

---

## 4. Decisões Técnicas Principais

| Decisão | Recomendação Adotada | Justificativa |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core 8.0 | Alta performance, tipagem forte e suporte nativo a SignalR e Rate Limiting |
| **Arquitetura** | Modular Monolith (Clean Architecture) | Baixo custo operacional inicial com fronteiras claras para futura extração |
| **Banco de Dados** | PostgreSQL | Robustez transacional, suporte a JSON e extensão geográfica PostGIS |
| **ORM** | Entity Framework Core 8 | Produtividade, Migrations versionadas e interceptação de auditoria |
| **Cache** | MemoryCache / Redis | Redução de latência em leituras frequentes de vitrine e cardápios |
| **Tempo Real** | ASP.NET Core SignalR | Atualização instantânea de pedidos e GPS de entrega |
| **Mensageria** | System.Threading.Channels / RabbitMQ | Desacoplamento de eventos sem sobrecarga no ambiente inicial |
| **Jobs Agendados** | BackgroundService (`PeriodicTimer`) | Execução in-process para renovação de assinaturas e limpeza de tokens |
| **Frontend** | Vue 3 + Vite + TypeScript + Pinia + Tailwind | Reatividade, tipagem estrita e construção modular de painéis por perfil |
| **Autenticação** | JWT + Refresh Token + BCrypt + RBAC | Controle de acesso stateless aliado a revogação segura de sessão |

---

## 5. Critérios para Extração Futura de Microsserviços

O sistema deve permanecer como **Modular Monolith** até que métricas objetivas justifiquem a extração individual de módulos (`Order Service`, `Payment Service`, `Subscription Service`, `Delivery Service`, `Notification Service`, `Financial Service`):

1. **Escala Assimétrica**: O módulo de rastreamento/entregas (`Delivery Service`) receber volume de escrita de GPS mais de 10x superior ao throughput transacional do restante da API.
2. **Isolamento de Falhas e Compliance**: O módulo de pagamentos e Ledger (`Payment / Financial Service`) exigir certificação ou rede segregada (PCI-DSS) independente do restante do marketplace.
3. **Autonomia de Deploy**: Múltiplas squads atuando simultaneamente com conflitos de janela de implantação.
