using PettyCashManager.Domain;
using PettyCashManager.Infrastructure;

namespace PettyCashManager.Services;

public sealed class AuthService
{
    private readonly IRepository<User> _userRepo;

    public AuthService(IRepository<User> userRepo)
    {
        _userRepo = userRepo;
    }

    public void SeedDemoUsers()
    {
        var users = _userRepo.GetAll().Data ?? new();
        if (users.Count > 0) return;

        _userRepo.Add(new User { Username = "req1", Password = "pass", DisplayName = "Ravi (Requester)", Role = UserRole.Requester });
        _userRepo.Add(new User { Username = "app1", Password = "pass", DisplayName = "Anita (Approver)", Role = UserRole.Approver });
        _userRepo.Add(new User { Username = "acc1", Password = "pass", DisplayName = "Suresh (Accountant)", Role = UserRole.Accountant });
        _userRepo.Add(new User { Username = "aud1", Password = "pass", DisplayName = "Divya (Auditor)", Role = UserRole.Auditor });
    }

    public User? Login(string username, string password)
    {
        var users = _userRepo.GetAll().Data ?? new();
        return users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
            && u.Password == password);
    }
}
