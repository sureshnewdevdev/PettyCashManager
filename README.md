# Petty Cash Manager (Console App) — C# OOP + Generics

This is a complete **menu-driven console application** demonstrating:
- OOP: Encapsulation, Inheritance, Polymorphism, Abstraction
- Generics: `IRepository<T>`, `InMemoryRepository<T>`, `Result<T>`, `ReportBuilder<T>`
- Real-world workflow: Petty cash fund, expense voucher, approvals, reimbursements, reports, audit trail

## Requirements
- .NET SDK 8.0 (or later)

## How to run
```bash
cd PettyCashManager.ConsoleApp
dotnet restore
dotnet run
```

## Demo users (pre-seeded)
- Requester:  `req1`  / `pass`
- Approver:   `app1`  / `pass`
- Accountant: `acc1`  / `pass`
- Auditor:    `aud1`  / `pass`

> You can switch users from the main menu any time.

## Core rules implemented
- Expense vouchers are created as **Pending**
- Only an **Approver** can Approve/Reject
- Fund balance is reduced only when an expense is **Approved**
- Reimbursements are **Auto-Approved** and increase fund balance immediately
- Every operation writes to an **Audit Log**
