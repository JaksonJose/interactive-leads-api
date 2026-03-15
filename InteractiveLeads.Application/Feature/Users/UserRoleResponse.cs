namespace InteractiveLeads.Application.Feature.Users
{
    public class UserRoleResponse
    {
        public Guid RoleId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class UserRolesResponse
    {
        public List<UserRoleResponse> UserRoles { get; set; } = [];
    }
}
