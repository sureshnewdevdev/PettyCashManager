using PettyCashManager.Domain;
using PettyCashManager.Infrastructure;

namespace PettyCashManager.Services;

public sealed class AuditService
{
    private readonly IRepository<AuditLogEntry> _auditRepo;

    public AuditService(IRepository<AuditLogEntry> auditRepo)
    {
        _auditRepo = auditRepo;
    }

    public void Log(string actor, string action, string entityName, string entityId, string details)
    {
        _auditRepo.Add(new AuditLogEntry
        {
            Actor = actor,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            Time = DateTime.Now
        });
    }

    public List<AuditLogEntry> GetAll() => _auditRepo.GetAll().Data ?? new();
}
