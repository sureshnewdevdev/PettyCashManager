namespace PettyCashManager.Domain;

public sealed class AuditLogEntry : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Time { get; set; } = DateTime.Now;
    public string Actor { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;      // e.g., CREATE, UPDATE, APPROVE
    public string EntityName { get; set; } = string.Empty;  // e.g., Fund, Transaction
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public override string ToString() => $"{Time:g} | {Actor} | {Action} | {EntityName}({EntityId}) | {Details}";
}
