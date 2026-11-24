using Marmitas.Domain.Entities;
using Marmitas.Domain.Enums;
using Marmitas.Domain.Exceptions;
using Marmitas.Domain.ValueObjects;

namespace Marmitas.UnitTests.Rules;

public class DomainBusinessRulesTests
{
    [Fact]
    public void Order_FluxoValidoDeStatus_DevePercorrerAteDelivered()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid());
        order.AddItem(new OrderItem(Guid.NewGuid(), "Marmita Frango", 25m, 2));

        Assert.Equal(50m, order.TotalAmount);
        Assert.Equal(OrderStatus.Created, order.Status);

        order.TransitionTo(OrderStatus.Paid);
        order.TransitionTo(OrderStatus.Confirmed);
        order.TransitionTo(OrderStatus.Preparing);
        order.TransitionTo(OrderStatus.ReadyForPickup);
        order.TransitionTo(OrderStatus.DriverAssigned);
        order.TransitionTo(OrderStatus.PickedUp);
        order.TransitionTo(OrderStatus.InTransit);
        order.TransitionTo(OrderStatus.Delivered);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotEmpty(order.DomainEvents);
    }

    [Fact]
    public void Order_TransicaoInvalidaDeDeliveredParaPreparing_DeveLancarDomainException()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid());
        order.TransitionTo(OrderStatus.Paid);
        order.TransitionTo(OrderStatus.ReadyForPickup);
        order.TransitionTo(OrderStatus.InTransit);
        order.TransitionTo(OrderStatus.Delivered);

        Assert.Throws<DomainException>(() => order.TransitionTo(OrderStatus.Preparing));
    }

    [Fact]
    public void Order_ComCupomETaxaDeEntrega_DeveCalcularTotalCorretamente()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid());
        order.AddItem(new OrderItem(Guid.NewGuid(), "Executiva Alcatra", 40m, 2));
        order.SetDeliveryFee(6.90m);
        order.ApplyCoupon("BEMVINDO10", 10m);

        Assert.Equal(80m, order.Subtotal);
        Assert.Equal(8m, order.DiscountAmount);
        Assert.Equal(6.90m, order.DeliveryFee);
        Assert.Equal(78.90m, order.TotalAmount);
    }

    [Fact]
    public void Subscription_UpgradeDePlano_DeveAtualizarFranquiaECalcularProrrata()
    {
        var sub = new Subscription
        {
            UserId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PlanName = "Plano Básico",
            MonthlyPrice = 200m,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = DateTime.UtcNow.AddDays(15),
            TotalMealsInCycle = 20,
            UsedMealsInCycle = 5
        };

        var premiumPlan = new Plan
        {
            Name = "Plano Premium",
            MealsPerWeek = 15,
            MealsPerMonth = 60,
            MonthlyPrice = 400m
        };

        var diff = sub.ChangePlan(premiumPlan);

        Assert.Equal("Plano Premium", sub.PlanName);
        Assert.Equal(60, sub.TotalMealsInCycle);
        Assert.Equal(55, sub.GetRemainingMeals());
        Assert.True(diff > 0m);
    }

    [Fact]
    public void Subscription_TrocaParaPlanoDeOutroRestaurante_DeveLancarDomainException()
    {
        var restaurantA = Guid.NewGuid();
        var restaurantB = Guid.NewGuid();

        var sub = new Subscription
        {
            UserId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PlanName = "Plano Restaurante A",
            RestaurantId = restaurantA,
            RestaurantName = "Restaurante A",
            MonthlyPrice = 200m,
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-10),
            NextBillingDate = DateTime.UtcNow.AddDays(20),
            TotalMealsInCycle = 20
        };

        var otherRestaurantPlan = new Plan
        {
            RestaurantId = restaurantB,
            RestaurantName = "Restaurante B",
            Name = "Plano Restaurante B",
            MealsPerWeek = 5,
            MealsPerMonth = 20,
            MonthlyPrice = 250m
        };

        Assert.Throws<DomainException>(() => sub.ChangePlan(otherRestaurantPlan));
    }

    [Fact]
    public void Delivery_AceiteConcorrentePorOutroEntregador_DeveLancarConflito()
    {
        var delivery = new Delivery
        {
            OrderId = Guid.NewGuid(),
            Status = "ReadyForPickup",
            ConfirmationPin = "4821"
        };

        var driver1 = Guid.NewGuid();
        var driver2 = Guid.NewGuid();

        delivery.AssignCourier(driver1);
        Assert.Throws<DomainException>(() => delivery.AssignCourier(driver2));
    }

    [Fact]
    public void Delivery_ConfirmacaoSemPinOuComPinIncorreto_DeveLancarExcecaoExcetoAdminOverride()
    {
        var delivery = new Delivery
        {
            OrderId = Guid.NewGuid(),
            Status = "InTransit",
            ConfirmationPin = "9182"
        };

        Assert.Throws<DomainException>(() => delivery.UpdateStatus("Delivered", null));
        Assert.Throws<DomainException>(() => delivery.UpdateStatus("Delivered", "0000"));

        delivery.UpdateStatus("Delivered", "9182");
        Assert.Equal("Delivered", delivery.Status);
        Assert.NotNull(delivery.DeliveredAt);

        var supportDelivery = new Delivery
        {
            OrderId = Guid.NewGuid(),
            Status = "InTransit",
            ConfirmationPin = "5432"
        };
        supportDelivery.UpdateStatus("Delivered", null, isAdminOverride: true);
        Assert.Equal("Delivered", supportDelivery.Status);
    }

    [Fact]
    public void GeoCalculator_DistanciaHaversineETaxa_DeveRetornarValoresConsistentes()
    {
        var distanceKm = GeoCalculator.CalculateDistanceKm(-26.9194, -49.0661, -26.8856, -49.0731);
        var fee = GeoCalculator.CalculateDeliveryFee(distanceKm);

        Assert.True(distanceKm > 2.5 && distanceKm < 5.0);
        Assert.True(fee > 5.90m);
    }

    [Fact]
    public void GeoCalculator_LocalizacaoBase_DeveSerBlumenauSC()
    {
        var fallbackPoint = GeoCalculator.ResolveAddressCoordinates(null, null, "Rua Sem Coordenadas, Blumenau/SC");
        Assert.InRange(fallbackPoint.Latitude, -27.1, -26.7);
        Assert.InRange(fallbackPoint.Longitude, -49.2, -48.9);
    }

    [Fact]
    public void GeoCalculator_EnderecoEmCaieirasSP_NaoDeveCairEmBlumenauEDeveRejeitarCoordenadaObsoleta()
    {
        var spPoint = GeoCalculator.ResolveAddressCoordinates(
            null,
            null,
            "Rua Principal",
            "Alpes de Caieiras",
            "Caieiras",
            "SP");

        Assert.InRange(spPoint.Latitude, -23.5, -23.2);
        Assert.InRange(spPoint.Longitude, -46.9, -46.6);
        Assert.True(GeoCalculator.CalculateDistanceKm(spPoint.Latitude, spPoint.Longitude, -26.9194, -49.0661) > 400.0);

        // Coordenada antiga salva erroneamente em Blumenau/SC deve ser descartada para Caieiras/SP
        var correctedPoint = GeoCalculator.ResolveAddressCoordinates(
            -26.9194,
            -49.0661,
            "Rua Principal",
            "Alpes de Caieiras",
            "Caieiras",
            "SP");
        Assert.InRange(correctedPoint.Latitude, -23.5, -23.2);

        // Texto com "Furb" mas explicitamente em Caieiras/SP não pode resolver para Blumenau
        var textParsed = GeoCalculator.ResolveAddressCoordinates(
            null,
            null,
            "Rest Furb - Alpes de Caieiras, Caieiras/SP");
        Assert.InRange(textParsed.Latitude, -23.5, -23.2);
    }

    [Fact]
    public void GeoCalculator_TempoEstimadoComOutrasParadas_DeveSomarTempoEDistanciaDasParadas()
    {
        var restaurant = new GeoPoint(-26.9194, -49.0661, "Restaurante");
        var customer = new GeoPoint(-26.8856, -49.0731, "Casa do Cliente");
        var stop1 = new GeoPoint(-26.9068, -49.0772, "Parada 1");
        var stop2 = new GeoPoint(-26.9094, -49.0867, "Parada 2");

        var directEta = GeoCalculator.EstimateDeliveryEta(restaurant, customer, "InTransit", restaurant);
        var multiStopEta = GeoCalculator.EstimateDeliveryEta(
            restaurant,
            customer,
            "InTransit",
            restaurant,
            new[] { stop1, stop2 });

        Assert.Equal(0, directEta.StopsAhead);
        Assert.Equal(2, multiStopEta.StopsAhead);
        Assert.True(multiStopEta.EstimatedMinutes > directEta.EstimatedMinutes);
        Assert.True(multiStopEta.TotalDistanceKm >= directEta.TotalDistanceKm);
        Assert.Equal(10, multiStopEta.StopsDelayMinutes);
        Assert.Contains("2 paradas anteriores", multiStopEta.RouteSummary);
    }

    [Fact]
    public void User_CincoFalhasDeLogin_DeveBloquearTemporariamenteEAnonimizarNaExclusao()
    {
        var user = new User("Maria Silva", "maria@teste.com", "11999998888", "hash");

        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin();

        Assert.True(user.IsLockedOut());

        user.ResetFailedLogins();
        Assert.False(user.IsLockedOut());

        user.Anonymize();
        Assert.Equal("Titular Anonimizado", user.Name);
        Assert.Null(user.Phone);
        Assert.Equal(UserStatus.Blocked, user.Status);
        Assert.NotNull(user.DeletedAt);
    }
}
