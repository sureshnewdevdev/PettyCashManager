using PettyCashManager.Domain;
using PettyCashManager.Infrastructure;

namespace PettyCashManager.Services;

public sealed class PettyCashService
{
    private readonly IRepository<PettyCashFund> _fundRepo;
    private readonly IRepository<Transaction> _txnRepo;
    private readonly AuditService _audit;

    public PettyCashService(
        IRepository<PettyCashFund> fundRepo,
        IRepository<Transaction> txnRepo,
        AuditService audit)
    {
        _fundRepo = fundRepo;
        _txnRepo = txnRepo;
        _audit = audit;
    }

    public Result<PettyCashFund> CreateFund(string actor, string name, decimal openingBalance)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<PettyCashFund>.Fail("Fund name is required");

        if (openingBalance < 0)
            return Result<PettyCashFund>.Fail("Opening balance cannot be negative");

        var fund = new PettyCashFund
        {
            Name = name.Trim(),
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            CreatedOn = DateTime.Now
        };

        var res = _fundRepo.Add(fund);
        if (res.Success)
            _audit.Log(actor, "CREATE", "Fund", fund.Id.ToString(), $"Created fund '{fund.Name}' with opening balance {fund.OpeningBalance:C}");

        return res;
    }

    public List<PettyCashFund> GetFunds() => _fundRepo.GetAll().Data ?? new();

    public PettyCashFund? GetFund(Guid fundId) => _fundRepo.GetById(fundId).Data;

    public Result<ExpenseTransaction> AddExpenseVoucher(
        string actor,
        Guid fundId,
        ExpenseCategory category,
        decimal amount,
        string voucherNo,
        string narration,
        DateTime date)
    {
        if (amount <= 0) return Result<ExpenseTransaction>.Fail("Amount must be greater than 0");
        if (string.IsNullOrWhiteSpace(voucherNo)) return Result<ExpenseTransaction>.Fail("Voucher number is required");
        if (date.Year < 2000 || date.Year > 2100) return Result<ExpenseTransaction>.Fail("Invalid date");

        var fund = GetFund(fundId);
        if (fund is null) return Result<ExpenseTransaction>.Fail("Fund not found");

        if (amount > fund.CurrentBalance)
            return Result<ExpenseTransaction>.Fail("Insufficient fund balance", $"Available: {fund.CurrentBalance:C}");

        var exp = new ExpenseTransaction
        {
            FundId = fundId,
            Category = category,
            Amount = amount,
            VoucherNumber = voucherNo.Trim(),
            Narration = narration?.Trim() ?? string.Empty,
            Date = date,
            RequestedBy = actor,
            Status = TransactionStatus.Pending
        };

        var res = _txnRepo.Add(exp);
        if (res.Success)
            _audit.Log(actor, "CREATE", "Expense", exp.Id.ToString(), $"Voucher {exp.VoucherNumber} | {exp.Category} | {exp.Amount:C} | Status: Pending");

        return res.Success
            ? Result<ExpenseTransaction>.Ok(exp, "Expense voucher created as Pending Approval")
            : Result<ExpenseTransaction>.Fail(res.Message, res.Errors.ToArray());
    }

    public Result<ReimbursementTransaction> AddReimbursement(
        string actor,
        Guid fundId,
        decimal amount,
        string referenceNo,
        string narration,
        DateTime date)
    {
        if (amount <= 0) return Result<ReimbursementTransaction>.Fail("Amount must be greater than 0");
        if (string.IsNullOrWhiteSpace(referenceNo)) return Result<ReimbursementTransaction>.Fail("Reference number is required");
        if (date.Year < 2000 || date.Year > 2100) return Result<ReimbursementTransaction>.Fail("Invalid date");

        var fund = GetFund(fundId);
        if (fund is null) return Result<ReimbursementTransaction>.Fail("Fund not found");

        var reimb = new ReimbursementTransaction
        {
            FundId = fundId,
            Amount = amount,
            ReferenceNumber = referenceNo.Trim(),
            Narration = narration?.Trim() ?? string.Empty,
            Date = date,
            RequestedBy = actor,
            Status = TransactionStatus.Approved,
            ProcessedBy = actor,
            ProcessedOn = DateTime.Now
        };

        var res = _txnRepo.Add(reimb);
        if (!res.Success)
            return Result<ReimbursementTransaction>.Fail(res.Message, res.Errors.ToArray());

        fund.CurrentBalance += amount;
        _fundRepo.Update(fund);

        _audit.Log(actor, "CREATE", "Reimbursement", reimb.Id.ToString(), $"Ref {reimb.ReferenceNumber} | {reimb.Amount:C} | Auto-Approved. Fund balance increased.");
        return Result<ReimbursementTransaction>.Ok(reimb, "Reimbursement added and fund balance updated");
    }

    public List<Transaction> GetTransactions(Guid fundId)
    {
        var all = _txnRepo.GetAll().Data ?? new();
        return all.Where(t => t.FundId == fundId).OrderByDescending(t => t.Date).ToList();
    }

    public List<ExpenseTransaction> GetPendingExpenses(Guid fundId)
    {
        var all = _txnRepo.GetAll().Data ?? new();
        return all
            .OfType<ExpenseTransaction>()
            .Where(e => e.FundId == fundId && e.Status == TransactionStatus.Pending)
            .OrderBy(e => e.Date)
            .ToList();
    }

    public Result<ExpenseTransaction> ApproveExpense(string actor, Guid expenseId)
    {
        var txnRes = _txnRepo.GetById(expenseId);
        if (!txnRes.Success || txnRes.Data is null)
            return Result<ExpenseTransaction>.Fail("Transaction not found");

        if (txnRes.Data is not ExpenseTransaction exp)
            return Result<ExpenseTransaction>.Fail("Only Expense transactions can be approved here");

        if (exp.Status != TransactionStatus.Pending)
            return Result<ExpenseTransaction>.Fail("This expense is already processed");

        var fund = GetFund(exp.FundId);
        if (fund is null) return Result<ExpenseTransaction>.Fail("Fund not found");

        if (exp.Amount > fund.CurrentBalance)
            return Result<ExpenseTransaction>.Fail("Insufficient balance to approve", $"Available: {fund.CurrentBalance:C}");

        exp.Status = TransactionStatus.Approved;
        exp.ProcessedBy = actor;
        exp.ProcessedOn = DateTime.Now;

        _txnRepo.Update(exp);

        fund.CurrentBalance -= exp.Amount;
        _fundRepo.Update(fund);

        _audit.Log(actor, "APPROVE", "Expense", exp.Id.ToString(), $"Approved voucher {exp.VoucherNumber}. Balance reduced by {exp.Amount:C}");
        return Result<ExpenseTransaction>.Ok(exp, "Expense Approved and balance updated");
    }

    public Result<ExpenseTransaction> RejectExpense(string actor, Guid expenseId, string reason)
    {
        var txnRes = _txnRepo.GetById(expenseId);
        if (!txnRes.Success || txnRes.Data is null)
            return Result<ExpenseTransaction>.Fail("Transaction not found");

        if (txnRes.Data is not ExpenseTransaction exp)
            return Result<ExpenseTransaction>.Fail("Only Expense transactions can be rejected here");

        if (exp.Status != TransactionStatus.Pending)
            return Result<ExpenseTransaction>.Fail("This expense is already processed");

        exp.Status = TransactionStatus.Rejected;
        exp.ProcessedBy = actor;
        exp.ProcessedOn = DateTime.Now;

        _txnRepo.Update(exp);

        _audit.Log(actor, "REJECT", "Expense", exp.Id.ToString(), $"Rejected voucher {exp.VoucherNumber}. Reason: {reason}");
        return Result<ExpenseTransaction>.Ok(exp, "Expense Rejected (no balance change)");
    }
}
