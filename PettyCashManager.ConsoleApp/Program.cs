using PettyCashManager.Domain;
using PettyCashManager.Infrastructure;
using PettyCashManager.Services;
using PettyCashManager.UI;

namespace PettyCashManager;

internal static class Program
{
    private static AuthService _auth = default!;
    private static PettyCashService _petty = default!;
    private static AuditService _audit = default!;

    private static User? _currentUser;
    private static PettyCashFund? _currentFund;

    private static void Main()
    {
        // Simple DI wiring (Console App)
        var userRepo = new InMemoryRepository<User>();
        var fundRepo = new InMemoryRepository<PettyCashFund>();

        // Generic repository storing polymorphic objects via base type
        var txnRepo = new InMemoryRepository<Transaction>();
        var auditRepo = new InMemoryRepository<AuditLogEntry>();

        _audit = new AuditService(auditRepo);
        _auth = new AuthService(userRepo);
        _auth.SeedDemoUsers();

        _petty = new PettyCashService(fundRepo, txnRepo, _audit);

        LoginScreen();

        while (true)
        {
            ConsoleHelpers.Header($"Petty Cash Manager | User: {_currentUser} | Fund: {(_currentFund?.Name ?? "Not Selected")}");
            ShowQuickStatus();

            Console.WriteLine("1) Create Fund");
            Console.WriteLine("2) Select Fund");
            Console.WriteLine("3) Add Expense Voucher (Requester)");
            Console.WriteLine("4) Approve/Reject Expenses (Approver)");
            Console.WriteLine("5) Add Reimbursement / Top-up (Accountant)");
            Console.WriteLine("6) View Fund Balance & Ledger");
            Console.WriteLine("7) Reports");
            Console.WriteLine("8) Audit Trail (Auditor)");
            Console.WriteLine("9) Switch User");
            Console.WriteLine("0) Exit");
            Console.WriteLine();

            var choice = ConsoleHelpers.ReadInt("Choose an option: ", 0, 9);
            switch (choice)
            {
                case 1: CreateFund(); break;
                case 2: SelectFund(); break;
                case 3: AddExpense(); break;
                case 4: Approvals(); break;
                case 5: AddReimbursement(); break;
                case 6: ViewLedger(); break;
                case 7: Reports(); break;
                case 8: AuditTrail(); break;
                case 9: LoginScreen(); break;
                case 0: return;
            }
        }
    }

    private static void ShowQuickStatus()
    {
        if (_currentFund is null)
        {
            Console.WriteLine("⚠ No fund selected. Create or select a fund to continue.");
        }
        else
        {
            _currentFund = _petty.GetFund(_currentFund.Id);
            Console.WriteLine($"Current Fund Balance: {_currentFund!.CurrentBalance:C}");
            var pending = _petty.GetPendingExpenses(_currentFund.Id).Count;
            if (pending > 0)
                Console.WriteLine($"Pending Expense Vouchers: {pending}");
        }
        Console.WriteLine();
    }

    private static void LoginScreen()
    {
        while (true)
        {
            ConsoleHelpers.Header("Login (Demo Users)");
            Console.WriteLine("Demo users: req1 / app1 / acc1 / aud1 (password: pass)");
            Console.WriteLine();

            var username = ConsoleHelpers.ReadRequired("Username: ");
            var password = ConsoleHelpers.ReadRequired("Password: ");

            var user = _auth.Login(username, password);
            if (user is null)
            {
                Console.WriteLine("Invalid username/password.");
                ConsoleHelpers.Pause();
                continue;
            }

            _currentUser = user;
            return;
        }
    }

    private static void CreateFund()
    {
        ConsoleHelpers.Header("Create Petty Cash Fund");

        if (_currentUser is null) return;

        var name = ConsoleHelpers.ReadRequired("Fund name: ");
        var opening = ConsoleHelpers.ReadDecimal("Opening balance: ", 0);

        var res = _petty.CreateFund(_currentUser.Username, name, opening);
        Console.WriteLine(res.Success ? $"✅ {res.Message}" : $"❌ {res.Message}");

        if (res.Success)
            _currentFund = res.Data;

        ConsoleHelpers.Pause();
    }

    private static void SelectFund()
    {
        ConsoleHelpers.Header("Select Fund");
        var funds = _petty.GetFunds();

        if (funds.Count == 0)
        {
            Console.WriteLine("No funds available. Create a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        for (int i = 0; i < funds.Count; i++)
            Console.WriteLine($"{i + 1}) {funds[i]}");

        Console.WriteLine();
        var idx = ConsoleHelpers.ReadInt("Select fund number: ", 1, funds.Count) - 1;
        _currentFund = funds[idx];

        Console.WriteLine($"✅ Selected fund: {_currentFund.Name}");
        ConsoleHelpers.Pause();
    }

    private static void AddExpense()
    {
        ConsoleHelpers.Header("Add Expense Voucher");

        if (_currentUser is null) return;
        if (_currentUser.Role != UserRole.Requester)
        {
            Console.WriteLine("❌ Only Requester can create expense vouchers (workflow rule).");
            ConsoleHelpers.Pause();
            return;
        }

        if (_currentFund is null)
        {
            Console.WriteLine("Select a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        _currentFund = _petty.GetFund(_currentFund.Id);
        Console.WriteLine($"Fund: {_currentFund!.Name} | Balance: {_currentFund.CurrentBalance:C}");
        Console.WriteLine();

        Console.WriteLine("Expense Category:");
        foreach (var cat in Enum.GetValues<ExpenseCategory>())
            Console.WriteLine($"{(int)cat}) {cat}");
        Console.WriteLine();

        var catChoice = ConsoleHelpers.ReadInt("Choose category: ", 1, Enum.GetValues<ExpenseCategory>().Length);
        var category = (ExpenseCategory)catChoice;

        var amount = ConsoleHelpers.ReadDecimal("Amount: ", 0);
        var voucher = ConsoleHelpers.ReadRequired("Voucher number: ");
        var narration = ConsoleHelpers.ReadRequired("Narration: ");
        var date = ConsoleHelpers.ReadDate("Date (e.g., 2026-01-21): ");

        var res = _petty.AddExpenseVoucher(_currentUser.Username, _currentFund.Id, category, amount, voucher, narration, date);
        Console.WriteLine(res.Success ? $"✅ {res.Message}" : $"❌ {res.Message}");
        if (!res.Success && res.Errors.Count > 0)
            Console.WriteLine("Details: " + string.Join(" | ", res.Errors));

        ConsoleHelpers.Pause();
    }

    private static void Approvals()
    {
        ConsoleHelpers.Header("Approve / Reject Pending Expenses");

        if (_currentUser is null) return;
        if (_currentUser.Role != UserRole.Approver)
        {
            Console.WriteLine("❌ Only Approver can approve/reject expenses.");
            ConsoleHelpers.Pause();
            return;
        }

        if (_currentFund is null)
        {
            Console.WriteLine("Select a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        while (true)
        {
            _currentFund = _petty.GetFund(_currentFund.Id);
            var pending = _petty.GetPendingExpenses(_currentFund!.Id);

            ConsoleHelpers.Header($"Pending Expenses | Fund: {_currentFund.Name} | Balance: {_currentFund.CurrentBalance:C}");

            if (pending.Count == 0)
            {
                Console.WriteLine("No pending expenses.");
                ConsoleHelpers.Pause();
                return;
            }

            for (int i = 0; i < pending.Count; i++)
                Console.WriteLine($"{i + 1}) {pending[i]}");

            Console.WriteLine();
            Console.WriteLine("A) Approve | R) Reject | B) Back");
            Console.Write("Choose action: ");
            var action = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

            if (action == "B") return;

            var idx = ConsoleHelpers.ReadInt("Select voucher number from list: ", 1, pending.Count) - 1;
            var selected = pending[idx];

            if (action == "A")
            {
                var res = _petty.ApproveExpense(_currentUser.Username, selected.Id);
                Console.WriteLine(res.Success ? $"✅ {res.Message}" : $"❌ {res.Message}");
                if (!res.Success && res.Errors.Count > 0)
                    Console.WriteLine("Details: " + string.Join(" | ", res.Errors));
            }
            else if (action == "R")
            {
                var reason = ConsoleHelpers.ReadRequired("Reason for rejection: ");
                var res = _petty.RejectExpense(_currentUser.Username, selected.Id, reason);
                Console.WriteLine(res.Success ? $"✅ {res.Message}" : $"❌ {res.Message}");
            }
            else
            {
                Console.WriteLine("Invalid action.");
            }

            ConsoleHelpers.Pause();
        }
    }

    private static void AddReimbursement()
    {
        ConsoleHelpers.Header("Add Reimbursement / Top-up");

        if (_currentUser is null) return;
        if (_currentUser.Role != UserRole.Accountant)
        {
            Console.WriteLine("❌ Only Accountant can add reimbursements/top-ups.");
            ConsoleHelpers.Pause();
            return;
        }

        if (_currentFund is null)
        {
            Console.WriteLine("Select a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        _currentFund = _petty.GetFund(_currentFund.Id);
        Console.WriteLine($"Fund: {_currentFund!.Name} | Current Balance: {_currentFund.CurrentBalance:C}");
        Console.WriteLine();

        var amount = ConsoleHelpers.ReadDecimal("Top-up amount: ", 0);
        var reference = ConsoleHelpers.ReadRequired("Reference number: ");
        var narration = ConsoleHelpers.ReadRequired("Narration: ");
        var date = ConsoleHelpers.ReadDate("Date (e.g., 2026-01-21): ");

        var res = _petty.AddReimbursement(_currentUser.Username, _currentFund.Id, amount, reference, narration, date);
        Console.WriteLine(res.Success ? $"✅ {res.Message}" : $"❌ {res.Message}");

        ConsoleHelpers.Pause();
    }

    private static void ViewLedger()
    {
        ConsoleHelpers.Header("Fund Balance & Ledger");

        if (_currentFund is null)
        {
            Console.WriteLine("Select a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        _currentFund = _petty.GetFund(_currentFund.Id);
        Console.WriteLine($"Fund: {_currentFund!.Name}");
        Console.WriteLine($"Balance: {_currentFund.CurrentBalance:C}");
        Console.WriteLine();

        var txns = _petty.GetTransactions(_currentFund.Id);
        if (txns.Count == 0)
        {
            Console.WriteLine("No transactions yet.");
            ConsoleHelpers.Pause();
            return;
        }

        Console.WriteLine("Filter? 1) No  2) By Status  3) By Date Range");
        var f = ConsoleHelpers.ReadInt("Choose: ", 1, 3);

        IEnumerable<Transaction> filtered = txns;

        if (f == 2)
        {
            Console.WriteLine("Status: 1) Pending 2) Approved 3) Rejected");
            var s = ConsoleHelpers.ReadInt("Choose status: ", 1, 3);
            var status = (TransactionStatus)s;
            filtered = filtered.Where(t => t.Status == status);
        }
        else if (f == 3)
        {
            var from = ConsoleHelpers.ReadDate("From date: ");
            var to = ConsoleHelpers.ReadDate("To date: ");
            if (to < from)
            {
                Console.WriteLine("Invalid range. 'To' must be >= 'From'.");
                ConsoleHelpers.Pause();
                return;
            }
            filtered = filtered.Where(t => t.Date.Date >= from.Date && t.Date.Date <= to.Date);
        }

        Console.WriteLine();
        foreach (var t in filtered.OrderByDescending(t => t.Date))
            Console.WriteLine(t);

        ConsoleHelpers.Pause();
    }

    private static void Reports()
    {
        ConsoleHelpers.Header("Reports");

        if (_currentFund is null)
        {
            Console.WriteLine("Select a fund first.");
            ConsoleHelpers.Pause();
            return;
        }

        var txns = _petty.GetTransactions(_currentFund.Id);

        Console.WriteLine("1) Daily Summary (Date totals)");
        Console.WriteLine("2) Category-wise Totals (Approved expenses)");
        Console.WriteLine("3) Pending Approvals");
        Console.WriteLine("0) Back");
        Console.WriteLine();

        var choice = ConsoleHelpers.ReadInt("Choose: ", 0, 3);
        if (choice == 0) return;

        if (choice == 1)
        {
            var from = ConsoleHelpers.ReadDate("From date: ");
            var to = ConsoleHelpers.ReadDate("To date: ");
            if (to < from) { Console.WriteLine("Invalid range."); ConsoleHelpers.Pause(); return; }

            var items = txns.Where(t => t.Date.Date >= from.Date && t.Date.Date <= to.Date);

            var lines = ReportBuilder<Transaction>.BuildLines(
                items.OrderBy(t => t.Date),
                t => $"{t.Date:yyyy-MM-dd} | {t.Type,-13} | {t.Amount,10:C} | {t.Status,-8} | {t.Narration}");

            Console.WriteLine();
            if (!lines.Any()) Console.WriteLine("(No data)");
            else lines.ForEach(Console.WriteLine);

            Console.WriteLine();
            Console.WriteLine($"Total Count: {items.Count()}");
            ConsoleHelpers.Pause();
        }
        else if (choice == 2)
        {
            var approvedExpenses = txns
                .OfType<ExpenseTransaction>()
                .Where(e => e.Status == TransactionStatus.Approved)
                .ToList();

            var grouped = approvedExpenses
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();

            Console.WriteLine();
            if (!grouped.Any())
            {
                Console.WriteLine("(No approved expenses)");
            }
            else
            {
                foreach (var g in grouped)
                    Console.WriteLine($"{g.Category,-15} | Count: {g.Count,3} | Total: {g.Total,10:C}");
            }

            ConsoleHelpers.Pause();
        }
        else if (choice == 3)
        {
            var pending = txns.OfType<ExpenseTransaction>()
                .Where(e => e.Status == TransactionStatus.Pending)
                .OrderBy(e => e.Date)
                .ToList();

            Console.WriteLine();
            if (!pending.Any()) Console.WriteLine("(No pending approvals)");
            else pending.ForEach(Console.WriteLine);

            ConsoleHelpers.Pause();
        }
    }

    private static void AuditTrail()
    {
        ConsoleHelpers.Header("Audit Trail");

        if (_currentUser is null) return;
        if (_currentUser.Role != UserRole.Auditor)
        {
            Console.WriteLine("❌ Only Auditor can view audit trail.");
            ConsoleHelpers.Pause();
            return;
        }

        var logs = _audit.GetAll().OrderByDescending(a => a.Time).ToList();
        if (!logs.Any())
        {
            Console.WriteLine("No audit logs found.");
            ConsoleHelpers.Pause();
            return;
        }

        Console.WriteLine("Filter? 1) No  2) By Actor  3) By Date Range");
        var f = ConsoleHelpers.ReadInt("Choose: ", 1, 3);

        IEnumerable<AuditLogEntry> filtered = logs;

        if (f == 2)
        {
            var actor = ConsoleHelpers.ReadRequired("Actor username: ");
            filtered = filtered.Where(l => l.Actor.Equals(actor, StringComparison.OrdinalIgnoreCase));
        }
        else if (f == 3)
        {
            var from = ConsoleHelpers.ReadDate("From date: ");
            var to = ConsoleHelpers.ReadDate("To date: ");
            if (to < from) { Console.WriteLine("Invalid range."); ConsoleHelpers.Pause(); return; }
            filtered = filtered.Where(l => l.Time.Date >= from.Date && l.Time.Date <= to.Date);
        }

        Console.WriteLine();
        foreach (var l in filtered.OrderByDescending(l => l.Time))
            Console.WriteLine(l);

        ConsoleHelpers.Pause();
    }
}
