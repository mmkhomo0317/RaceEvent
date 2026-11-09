namespace RaceEvent.Models.ViewModels
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public IEnumerable<string> Roles { get; set; } = new List<string> ();
    }
}
