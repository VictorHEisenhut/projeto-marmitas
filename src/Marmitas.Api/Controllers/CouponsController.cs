using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marmitas.Api.Controllers;

[Route("api/cupons")]
[ApiController]
public class CouponsController : ControllerBase
{
    private static readonly List<CouponDto> Coupons = new()
    {
        new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = "MARMITA10",
            DiscountPercentage = 10,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        }
    };

    private static readonly List<CouponUsageDto> Usages = new();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        return Ok(Coupons);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetById(Guid id)
    {
        var coupon = Coupons.FirstOrDefault(c => c.Id == id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        return Ok(coupon);
    }

    [HttpPost]
    [Authorize]
    public IActionResult Create([FromBody] SaveCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new Exception("O código do cupom é obrigatório.");

        var coupon = new CouponDto
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpper(),
            DiscountPercentage = request.DiscountPercentage,
            IsActive = true,
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddMonths(1)
        };

        Coupons.Add(coupon);
        return StatusCode(201, coupon);
    }

    [HttpPut("{id}")]
    [Authorize]
    public IActionResult Update(Guid id, [FromBody] SaveCouponRequest request)
    {
        var coupon = Coupons.FirstOrDefault(c => c.Id == id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Code))
            coupon.Code = request.Code.Trim().ToUpper();
        coupon.DiscountPercentage = request.DiscountPercentage;
        if (request.ExpiresAt.HasValue)
            coupon.ExpiresAt = request.ExpiresAt.Value;

        return Ok(coupon);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public IActionResult Delete(Guid id)
    {
        var coupon = Coupons.FirstOrDefault(c => c.Id == id);
        if (coupon == null)
            throw new Exception("Cupom não encontrado.");

        Coupons.Remove(coupon);
        return NoContent();
    }

    [HttpPost("validar")]
    [Authorize]
    public IActionResult ValidateCoupon([FromBody] ValidateCouponRequest request)
    {
        var coupon = Coupons.FirstOrDefault(c =>
            c.Code.Equals(request.Code?.Trim(), StringComparison.OrdinalIgnoreCase) && c.IsActive);

        if (coupon == null || coupon.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Cupom inválido ou expirado.");

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
    [Authorize]
    public IActionResult GetUsages(Guid id)
    {
        var list = Usages.Where(u => u.CouponId == id).ToList();
        return Ok(list);
    }
}

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class CouponUsageDto
{
    public Guid Id { get; set; }
    public Guid CouponId { get; set; }
    public Guid UserId { get; set; }
    public DateTime UsedAt { get; set; }
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
