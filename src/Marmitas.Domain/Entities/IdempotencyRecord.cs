namespace Marmitas.Domain.Entities;

public class IdempotencyRecord : Entity
{
    public string Key { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
