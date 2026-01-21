namespace PettyCashManager.Domain;

public abstract class Transaction : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FundId { get; set; }
    public TransactionType Type { get; protected set; }

    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Narration { get; set; } = string.Empty;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public string RequestedBy { get; set; } = string.Empty;
    public string? ProcessedBy { get; set; }
    public DateTime? ProcessedOn { get; set; }

    public override string ToString()
        => $"{Type} | {Amount:C} | {Date:g} | {Status} | By: {RequestedBy} | {Narration}";
}

public sealed class ExpenseTransaction : Transaction
{
    public ExpenseTransaction()
    {
        Type = TransactionType.Expense;
        Status = TransactionStatus.Pending;
    }

    public ExpenseCategory Category { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;

    public override string ToString()
        => $"Expense | {Category} | Voucher: {VoucherNumber} | {Amount:C} | {Date:g} | {Status} | Req: {RequestedBy} | {Narration}";
}

public sealed class ReimbursementTransaction : Transaction
{
    public ReimbursementTransaction()
    {
        Type = TransactionType.Reimbursement;
        Status = TransactionStatus.Approved; // auto-approved by rule
    }

    public string ReferenceNumber { get; set; } = string.Empty;

    public override string ToString()
        => $"Reimb | Ref: {ReferenceNumber} | {Amount:C} | {Date:g} | {Status} | By: {RequestedBy} | {Narration}";
}
