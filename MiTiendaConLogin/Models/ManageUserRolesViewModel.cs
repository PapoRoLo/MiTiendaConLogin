namespace MiTiendaConLogin.Models
{
    public class ManageUserRolesViewModel
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public List<RoleViewModel>? Roles { get; set; }
    }

    public class RoleViewModel
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}