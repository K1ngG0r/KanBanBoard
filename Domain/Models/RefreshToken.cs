namespace Domain.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }
    public Guid AccountId { get; set; }
    public Account Account { get; set; }
    public DateTime ExpirateAt { get; set; }
    public bool IsExpired { get; set; }

    public RefreshToken()
    {
        Id = Guid.NewGuid();
    }

    public RefreshToken(Guid userId, DateTime expirateAt, string token)
    {
        Id = Guid.NewGuid();
        Token = token;
        AccountId = userId;
        ExpirateAt = expirateAt;
        IsExpired = false;
    }

    public void Invalidate()
    {
        if (IsExpired) return;
        IsExpired = true;
    }
}
