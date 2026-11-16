-- ============================================================================
-- SEED DE RESTAURANTES FICTÍCIOS, CARDÁPIOS COMPLETOS E CLIENTES (COM E SEM PLANO)
-- ============================================================================

DO $$
DECLARE
    v_pwd_hash TEXT := '$2a$11$TGF3dQvFLXNi7cFrzvRO/.LlO40/vpLQdwPqV8bsN1h/4rzr2dScW'; -- admin123
    v_role_owner UUID := 'a1b2c3d4-0001-0000-0000-000000000002';
    v_role_customer UUID := 'a1b2c3d4-0001-0000-0000-000000000001';

    -- Categorias padrão
    c_tradicionais UUID := '00000000-0000-0000-0002-000000000001';
    c_fitness UUID := '00000000-0000-0000-0002-000000000002';
    c_executiva UUID := '00000000-0000-0000-0002-000000000003';
    c_vegetariana UUID := '00000000-0000-0000-0002-000000000004';
    c_bebidas UUID := '00000000-0000-0000-0002-000000000005';

    -- IDs dos Donos
    u_dono1 UUID := '11111111-1111-1111-1111-111111111111';
    u_dono2 UUID := '22222222-2222-2222-2222-222222222222';
    u_dono3 UUID := '33333333-3333-3333-3333-333333333333';

    -- IDs dos Restaurantes
    r_sabores UUID := 'aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa';
    r_fitlife UUID := 'bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb';
    r_boteco  UUID := 'cccccccc-3333-3333-3333-cccccccccccc';

    -- IDs dos Clientes
    u_cliente_plano UUID := '44444444-4444-4444-4444-444444444444';
    u_cliente_avulso UUID := '55555555-5555-5555-5555-555555555555';

    -- IDs dos Planos
    p_essencial UUID := '99999999-0001-0000-0000-000000000001';
    p_pro UUID := '99999999-0002-0000-0000-000000000002';
    p_vip UUID := '99999999-0003-0000-0000-000000000003';

    s_assinatura UUID := '88888888-0001-0000-0000-000000000001';
BEGIN
    -- 1. CRIAÇÃO DOS DONOS DOS RESTAURANTES
    INSERT INTO users (id, name, email, phone, password_hash, status, created_at, updated_at) VALUES
        (u_dono1, 'Chef Rodrigo Siqueira', 'contato@saboresdavila.com.br', '(11) 98765-4321', v_pwd_hash, 'Active', NOW(), NOW()),
        (u_dono2, 'Dra. Camila Nogueira', 'atendimento@fitlifegourmet.com.br', '(11) 97654-3210', v_pwd_hash, 'Active', NOW(), NOW()),
        (u_dono3, 'Marcos Oliveira', 'marcos@cantinapaulista.com.br', '(11) 96543-2109', v_pwd_hash, 'Active', NOW(), NOW())
    ON CONFLICT (email) DO NOTHING;

    INSERT INTO user_roles (user_id, role_id) VALUES
        (u_dono1, v_role_owner),
        (u_dono2, v_role_owner),
        (u_dono3, v_role_owner)
    ON CONFLICT DO NOTHING;

    -- 2. CRIAÇÃO DOS 3 RESTAURANTES COM NOMES REAIS
    INSERT INTO restaurants (id, name, description, document, owner_id, status, created_at, updated_at) VALUES
        (r_sabores, 'Sabores da Vila Gastronomia', 'Culinária caseira afetiva com ingredientes frescos selecionados do dia, temperos naturais e receitas de família.', '34.892.124/0001-50', u_dono1, 'Approved', NOW(), NOW()),
        (r_fitlife, 'FitLife Marmitaria & Nutrição', 'Alimentação saudável, funcional e balanceada por nutricionistas. Baixo sódio, proteínas nobres e opções low carb.', '41.520.891/0001-83', u_dono2, 'Approved', NOW(), NOW()),
        (r_boteco, 'Cantina & Bistrô Paulista', 'Pratos clássicos paulistanos, cortes nobres grelhados na hora, massas artesanais e marmitas executivas de alto padrão.', '28.341.902/0001-12', u_dono3, 'Approved', NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        description = EXCLUDED.description,
        document = EXCLUDED.document,
        status = 'Approved';

    -- 3. PRODUTOS DO RESTAURANTE 1: "Sabores da Vila Gastronomia"
    -- 1) Tradicionais
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'a1000000-0000-0000-0000-000000000001', r_sabores, c_tradicionais,
        'Marmita Feijoada Completa da Vila',
        'Feijoada tradicional com lombo, costelinha e paio, acompanhada de arroz branco, couve refogada no alho, farofa crocante e fatias de laranja.',
        29.90, true, true, false, 0.00,
        '[{"Name":"Farofa Extra de Bacon","ExtraPrice":3.50,"MaxQuantity":2},{"Name":"Molho de Pimenta Artesanal","ExtraPrice":0.00,"MaxQuantity":1},{"Name":"Torresmo Crocante","ExtraPrice":4.50,"MaxQuantity":2}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 2) Fitness & Low Carb
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'a1000000-0000-0000-0000-000000000002', r_sabores, c_fitness,
        'Frango ao Molho de Ervas com Purê de Mandioquinha',
        'Peito de frango marinado em ervas finas, purê aveludado de mandioquinha e brócolis cozido no vapor.',
        27.50, true, true, false, 0.00,
        '[{"Name":"Filé de Frango Extra 100g","ExtraPrice":5.00,"MaxQuantity":2},{"Name":"Mix de Castanhas","ExtraPrice":4.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 3) Executiva (Exclusiva avulsa: NÃO disponível em plano)
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'a1000000-0000-0000-0000-000000000003', r_sabores, c_executiva,
        'Executivo de Picanha Grill com Vinagrete e Fritas',
        'Corte nobre de picanha grelhada na manteiga de garrafa, arroz branco, feijão carioquinha, farofa de ovos e batatas rústicas.',
        42.90, false, true, true, 2.00,
        '[{"Name":"Queijo Coalho Grelhado","ExtraPrice":6.00,"MaxQuantity":2},{"Name":"Ovo Caipira Estalado","ExtraPrice":3.00,"MaxQuantity":2}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 4) Vegetariana & Vegana
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'a1000000-0000-0000-0000-000000000004', r_sabores, c_vegetariana,
        'Moqueca de Palmito Pupunha e Banana da Terra',
        'Moqueca cremosa com leite de coco artesanal, azeite de dendê, pimentões coloridos, servida com arroz com gergelim e farofa de dendê.',
        28.00, true, true, false, 0.00,
        '[{"Name":"Porção Extra de Banana da Terra","ExtraPrice":3.50,"MaxQuantity":1},{"Name":"Pimenta Biquinho","ExtraPrice":0.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 5) Bebidas & Sobremesas
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'a1000000-0000-0000-0000-000000000005', r_sabores, c_bebidas,
        'Suco Natural de Laranja com Acerola (500ml)',
        'Suco fresco 100% natural, sem conservantes e extraído na hora.',
        9.50, true, false, false, 0.00,
        '[]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;


    -- 4. PRODUTOS DO RESTAURANTE 2: "FitLife Marmitaria & Nutrição"
    -- 1) Tradicionais
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'b2000000-0000-0000-0000-000000000001', r_fitlife, c_tradicionais,
        'Carne de Panela Desfiada com Arroz Integral e Feijão Preto',
        'Músculo bovino cozido lentamente em fogo brando com cebola roxa e tomates, servido com arroz integral e legumes.',
        28.90, true, true, false, 0.00,
        '[{"Name":"Ovo Cozido","ExtraPrice":2.50,"MaxQuantity":2},{"Name":"Chips de Mandioca Assada","ExtraPrice":3.50,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 2) Fitness & Low Carb
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'b2000000-0000-0000-0000-000000000002', r_fitlife, c_fitness,
        'Salmão Grelhado com Arroz de Couve-Flor e Aspargos',
        'Posta de salmão chileno temperado com raspas de limão siciliano, arroz leve de couve-flor e aspargos salteados no azeite extravirgem.',
        39.90, true, true, false, 0.00,
        '[{"Name":"Molho de Maracujá Low Carb","ExtraPrice":2.00,"MaxQuantity":1},{"Name":"Porção Extra de Aspargos","ExtraPrice":6.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 3) Executiva (Exclusiva avulsa: NÃO disponível em plano)
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'b2000000-0000-0000-0000-000000000003', r_fitlife, c_executiva,
        'Medalhão de Mignon ao Poivre com Purê de Cará Trufado',
        'Mignon alto grelhado com molho cremoso de pimenta verde e azeite trufado, servido com purê rústico de cará.',
        46.00, false, true, true, 2.50,
        '[{"Name":"Cogumelos Paris Salteados","ExtraPrice":7.00,"MaxQuantity":2}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 4) Vegetariana & Vegana
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'b2000000-0000-0000-0000-000000000004', r_fitlife, c_vegetariana,
        'Curry Cremoso de Grão-de-Bico com Espinafre e Quinoa Real',
        'Grão-de-bico macio preparado ao curry indiano, leite de coco fresco, espinafre orgânico e mix de quinoa tricolor.',
        26.50, true, true, false, 0.00,
        '[{"Name":"Tofu Grelhado Defumado","ExtraPrice":4.50,"MaxQuantity":2},{"Name":"Sementes de Abóbora Tostadas","ExtraPrice":2.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 5) Bebidas & Sobremesas
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'b2000000-0000-0000-0000-000000000005', r_fitlife, c_bebidas,
        'Kombucha Artesanal de Hibisco e Gengibre (350ml)',
        'Bebida probiótica fermentada naturalmente, frisante e refrescante.',
        12.00, true, false, false, 0.00,
        '[]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;


    -- 5. PRODUTOS DO RESTAURANTE 3: "Cantina & Bistrô Paulista"
    -- 1) Tradicionais
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'c3000000-0000-0000-0000-000000000001', r_boteco, c_tradicionais,
        'Bife à Parmegiana da Cantina com Tagliolini na Manteiga',
        'Filé empanado crocante, coberto com molho de tomates frescos da horta e queijo mussarela gratinado, com massa artesanal fresca.',
        32.00, true, true, false, 0.00,
        '[{"Name":"Mussarela Gratinada em Dobro","ExtraPrice":5.00,"MaxQuantity":2},{"Name":"Queijo Parmesão Ralado Faixa Azul","ExtraPrice":3.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 2) Fitness & Low Carb
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'c3000000-0000-0000-0000-000000000002', r_boteco, c_fitness,
        'Sobrecoxa Desossada Grelhada com Legumes ao Pesto',
        'Sobrecoxa dourada marinada no vinho branco e alecrim, acompanhada de abobrinha, cenoura e berinjela grelhadas ao pesto genovês.',
        25.00, true, true, false, 0.00,
        '[{"Name":"Porção Extra de Pesto Artesanal","ExtraPrice":3.00,"MaxQuantity":1},{"Name":"Ovo Poché","ExtraPrice":3.50,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 3) Executiva (Exclusiva avulsa: NÃO disponível em plano)
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'c3000000-0000-0000-0000-000000000003', r_boteco, c_executiva,
        'Risoto de Filé Mignon ao Funghi Secchi',
        'Arroz arbóreo italiano cremoso cozido com caldo artesanal de carne, tiras macias de mignon e funghi porcini hidratado no vinho tinto.',
        44.50, false, true, true, 1.50,
        '[{"Name":"Azeite Trufado Extra","ExtraPrice":5.00,"MaxQuantity":1},{"Name":"Castanhas Crocantes","ExtraPrice":3.00,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 4) Vegetariana & Vegana
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'c3000000-0000-0000-0000-000000000004', r_boteco, c_vegetariana,
        'Lasanha Artesanal de Berinjela com Ricota de Castanhas',
        'Fatias finas de berinjela grelhada intercaladas com molho pomodoro rústico, folhas de manjericão e queijo cremoso vegano de castanha.',
        27.90, true, true, false, 0.00,
        '[{"Name":"Molho Pomodoro Extra","ExtraPrice":2.50,"MaxQuantity":1}]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;

    -- 5) Bebidas & Sobremesas
    INSERT INTO products (id, restaurant_id, category_id, name, description, price, available_in_plans, allows_cutlery, cutlery_has_extra_cost, cutlery_price, addons_json, is_active, created_at, updated_at)
    VALUES (
        'c3000000-0000-0000-0000-000000000005', r_boteco, c_bebidas,
        'Pudim Tradicional de Leite Condensado com Fava de Baunilha',
        'Pudim clássico sem furinhos, textura aveludada e calda de caramelo brilhante.',
        11.50, true, true, false, 0.00,
        '[]',
        true, NOW(), NOW()
    ) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;


    -- 6. PLANOS DE ASSINATURA MENSAIS DISPONÍVEIS NA PLATAFORMA
    INSERT INTO plans (id, name, description, meals_per_week, monthly_price, is_active, created_at, updated_at) VALUES
        (p_essencial, 'Plano Mensal Essencial (5 marmitas/sem)', 'Ideal para almoços de segunda a sexta. 20 refeições completas no mês.', 5, 349.90, true, NOW(), NOW()),
        (p_pro, 'Plano Mensal Fitness Pro (7 marmitas/sem)', 'Almoço e finais de semana fitness. 28 refeições saudáveis no mês.', 7, 469.90, true, NOW(), NOW()),
        (p_vip, 'Plano Mensal Casal / Duplo (10 marmitas/sem)', 'Perfeito para compartilhar em família. 40 marmitas no mês.', 10, 649.90, true, NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        description = EXCLUDED.description,
        monthly_price = EXCLUDED.monthly_price;


    -- 7. CRIAÇÃO DOS CLIENTES:
    -- Cliente 1: COM PLANO CONTRATADO
    INSERT INTO users (id, name, email, phone, password_hash, status, created_at, updated_at) VALUES
        (u_cliente_plano, 'Lucas Mendes (Assinante Ativo)', 'lucas.assinante@gmail.com', '(11) 98111-2233', v_pwd_hash, 'Active', NOW(), NOW())
    ON CONFLICT (email) DO NOTHING;

    INSERT INTO user_roles (user_id, role_id) VALUES
        (u_cliente_plano, v_role_customer)
    ON CONFLICT DO NOTHING;

    -- Atribuição da assinatura ativa para Lucas Mendes
    INSERT INTO subscriptions (id, user_id, plan_id, plan_name, monthly_price, status, start_date, next_billing_date, created_at, updated_at) VALUES
        (s_assinatura, u_cliente_plano, p_essencial, 'Plano Mensal Essencial (5 marmitas/sem)', 349.90, 'Active', NOW(), NOW() + INTERVAL '1 month', NOW(), NOW())
    ON CONFLICT (id) DO UPDATE SET status = 'Active', next_billing_date = NOW() + INTERVAL '1 month';


    -- Cliente 2: SEM PLANO (APENAS COMPRA AVULSA)
    INSERT INTO users (id, name, email, phone, password_hash, status, created_at, updated_at) VALUES
        (u_cliente_avulso, 'Beatriz Santana (Compra Avulsa)', 'beatriz.avulsa@gmail.com', '(11) 98222-3344', v_pwd_hash, 'Active', NOW(), NOW())
    ON CONFLICT (email) DO NOTHING;

    INSERT INTO user_roles (user_id, role_id) VALUES
        (u_cliente_avulso, v_role_customer)
    ON CONFLICT DO NOTHING;

END $$;
