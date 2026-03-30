namespace InteractiveLeads.Application.Interfaces;

/// <summary>Applies template messages from <c>interactive-template-inbound</c> (<c>template</c>, <c>template_synced</c>).</summary>
public interface ITemplateInboundMessageHandler
{
    /// <summary>Returns true when the message was handled (ACK); false to retry (NACK requeue).</summary>
    Task<bool> TryHandleAsync(string jsonBody, CancellationToken cancellationToken);
}
