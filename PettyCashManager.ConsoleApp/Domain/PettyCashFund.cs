namespace PettyCashManager.Domain;

public sealed class PettyCashFund : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    /// <summary>
    /// Current balance is tracked by service rules. (OpeningBalance + Approved reimbursements - Approved expenses)
    /// </summary>
    public decimal CurrentBalance { get; set; }

    public override string ToString() => $"{Name} | Balance: {CurrentBalance:C} | Opened: {CreatedOn:d}";
}
