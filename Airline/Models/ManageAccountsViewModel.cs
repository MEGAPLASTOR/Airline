using Airline.Models;

namespace Airline.ViewModels.Admin
{
    public class ManageAccountsViewModel
    {
        public List<User> Accounts { get; set; } = new();

        public int TotalAccounts => Accounts.Count;
        public int TotalAdmins => Accounts.Count(x => (x.Role ?? "").ToUpper() == "ADMIN");
        public int TotalUsers => Accounts.Count(x => (x.Role ?? "").ToUpper() == "USER");
        public int NewThisMonth => Accounts.Count(x =>
            x.CreatedAt.HasValue &&
            x.CreatedAt.Value.Month == DateTime.Now.Month &&
            x.CreatedAt.Value.Year == DateTime.Now.Year);
    }
}