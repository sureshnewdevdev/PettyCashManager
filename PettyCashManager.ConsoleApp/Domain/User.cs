namespace PettyCashManager.Domain;

public sealed class User : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // demo-only
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public override string ToString() => $"{DisplayName} ({Username}) - {Role}";
}
