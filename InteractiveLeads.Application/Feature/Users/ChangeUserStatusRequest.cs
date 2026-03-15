namespace InteractiveLeads.Application.Feature.Users
{
    public class ChangeUserStatusRequest
    {
        public Guid UserId { get; set; }
        public bool Activation { get; set; }
    }
}
