using InteractiveLeads.Application.Feature.Chat.Messages.Outbound;

namespace InteractiveLeads.Application.Messaging.Contracts;

/// <summary>Outbound message to external channel workers (e.g. n8n). Body matches prior HTTP JSON shape.</summary>
public sealed record OutboundMessageDispatch(OutboundMessageContract Message);
