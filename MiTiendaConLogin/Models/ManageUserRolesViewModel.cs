namespace MiTiendaConLogin.Models
{
    // Esta clase es para la vista principal
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<RoleViewModel> Roles { get; set; }
    }

    // Esta clase representa cada checkbox
    public class RoleViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; } // El checkbox
    }
}