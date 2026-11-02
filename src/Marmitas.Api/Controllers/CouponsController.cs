using System.Security.Claims;
using Marmitas.Application.Common.Interfaces;
using Marmitas.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/cupons")]
[ApiController]
public class CouponsController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptions;

    public CouponsController(ISubscriptionRepository subscriptions)
    {
        _subscriptions = subscriptions;
    }

    [HttpGet]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetAll()
    {
        var coupons = await _subscriptions.GetAllCouponsAsync();
        return Ok(coupons);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var coupon = await _subscriptions.GetCouponByIdAsync(id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        return Ok(coupon);
    }

    [HttpPost]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Create([FromBody] SaveCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new Exception("O código do cupom é obrigatório.");

        var coupon = new Coupon
        {
            Code = request.Code.Trim().ToUpper(),
            DiscountPercentage = request.DiscountPercentage,
            IsActive = true,
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddMonths(1)
        };

        await _subscriptions.AddCouponAsync(coupon);
        await _subscriptions.SaveChangesAsync();

        return StatusCode(201, coupon);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveCouponRequest request)
    {
        var coupon = await _subscriptions.GetCouponByIdAsync(id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Code))
            coupon.Code = request.Code.Trim().ToUpper();
        coupon.DiscountPercentage = request.DiscountPercentage;
        if (request.ExpiresAt.HasValue)
            coupon.ExpiresAt = request.ExpiresAt.Value;

        await _subscriptions.SaveChangesAsync();
        return Ok(coupon);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var coupon = await _subscriptions.GetCouponByIdAsync(id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        _subscriptions.RemoveCoupon(coupon);
        await _subscriptions.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("validar")]
    [Authorize(Roles = "customer,admin,super_admin")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new Exception("Informe o código do cupom.");

        var coupon = await _subscriptions.GetCouponByCodeAsync(request.Code);
        if (coupon == null || coupon.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Cupom inválido ou expirado.");

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId))
        {
            await _subscriptions.AddCouponUsageAsync(new CouponUsage
            {
                CouponId = coupon.Id,
                UserId = userId,
                UsedAt = DateTime.UtcNow
            });
            await _subscriptions.SaveChangesAsync();
        }

        var discount = Math.Round(request.OrderAmount * (coupon.DiscountPercentage / 100m), 2);
        return Ok(new
        {
            valid = true,
            couponId = coupon.Id,
            code = coupon.Code,
            discountPercentage = coupon.DiscountPercentage,
            discountAmount = discount,
            finalAmount = Math.Max(0, request.OrderAmount - discount)
        });
    }

    [HttpGet("{id}/usos")]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> GetUsages(Guid id)
    {
        var list = await _subscriptions.GetCouponUsagesAsync(id);
        return Ok(list);
    }
}

public class SaveCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ValidateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
}
