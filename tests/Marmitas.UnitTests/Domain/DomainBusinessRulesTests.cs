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
        var distanceKm = GeoCalculator.CalculateDistanceKm(-23.5614, -46.6559, -23.5890, -46.6365);
        var fee = GeoCalculator.CalculateDeliveryFee(distanceKm);

        Assert.True(distanceKm > 2.5 && distanceKm < 5.0);
        Assert.True(fee > 5.90m);
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
