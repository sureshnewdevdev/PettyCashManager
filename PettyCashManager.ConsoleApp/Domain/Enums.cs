namespace PettyCashManager.Domain;

public enum UserRole
{
    Requester = 1,
    Approver = 2,
    Accountant = 3,
    Auditor = 4
}

public enum TransactionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum TransactionType
{
    Expense = 1,
    Reimbursement = 2
}

public enum ExpenseCategory
{
    Stationery = 1,
    Travel = 2,
    Refreshments = 3,
    Courier = 4,
    Misc = 5
}
