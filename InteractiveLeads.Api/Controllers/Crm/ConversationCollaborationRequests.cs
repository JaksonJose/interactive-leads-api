namespace InteractiveLeads.Api.Controllers.Crm;

public sealed class AssignResponsibleRequest
{
    public Guid ResponsibleUserId { get; set; }
}

public sealed class TransferResponsibleRequest
{
    public Guid NewResponsibleUserId { get; set; }
}

public sealed class AddParticipantRequestBody
{
    public Guid UserId { get; set; }
}
