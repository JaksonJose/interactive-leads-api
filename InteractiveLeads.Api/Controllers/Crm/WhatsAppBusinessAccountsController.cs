using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>CRM WhatsApp Business Account (WABA): templates are scoped per WABA, not per phone number integration.</summary>
[Authorize(Roles = "Owner,Manager")]
public sealed class WhatsAppBusinessAccountsController : BaseApiController
{
    /// <summary>All WABAs for the company (including accounts with no phone numbers yet).</summary>
    [HttpGet]
    [OpenApiOperation("List WhatsApp Business Accounts for company")]
    public async Task<IActionResult> ListAsync()
    {
        var response = await Sender.Send(new ListWhatsAppBusinessAccountsQuery());
        return Ok(response);
    }

    /// <summary>Register a WABA (Meta id) before adding phone number integrations.</summary>
    [HttpPost]
    [OpenApiOperation("Create WhatsApp Business Account")]
    public async Task<IActionResult> CreateAccountAsync([FromBody] CreateWhatsAppBusinessAccountRequest request)
    {
        var response = await Sender.Send(new CreateWhatsAppBusinessAccountCommand { Account = request });
        return Ok(response);
    }

    /// <summary>Get a WABA linked to the current company (for labels / breadcrumbs).</summary>
    [HttpGet("{wabaId:guid}")]
    [OpenApiOperation("Get WhatsApp Business Account by id")]
    public async Task<IActionResult> GetAsync(Guid wabaId)
    {
        var response = await Sender.Send(new GetWhatsAppBusinessAccountQuery { WhatsAppBusinessAccountId = wabaId });
        return Ok(response);
    }

    /// <summary>List cached message templates for the WABA.</summary>
    [HttpGet("{wabaId:guid}/templates")]
    [OpenApiOperation("List WhatsApp message templates for WABA")]
    public async Task<IActionResult> ListTemplatesAsync(Guid wabaId)
    {
        var response = await Sender.Send(new ListWhatsAppTemplatesQuery { WhatsAppBusinessAccountId = wabaId });
        return Ok(response);
    }

    /// <summary>Submit a new template (queued to <c>interactive-template-outbound</c> when RabbitMQ is enabled).</summary>
    [HttpPost("{wabaId:guid}/templates")]
    [OpenApiOperation("Create WhatsApp message template for WABA")]
    public async Task<IActionResult> CreateTemplateAsync(Guid wabaId, [FromBody] CreateWhatsAppTemplateRequest request)
    {
        var response = await Sender.Send(new CreateWhatsAppTemplateCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            Template = request
        });
        return Ok(response);
    }

    /// <summary>Get one template (for edit).</summary>
    [HttpGet("{wabaId:guid}/templates/{templateId:guid}")]
    [OpenApiOperation("Get WhatsApp message template by id")]
    public async Task<IActionResult> GetTemplateAsync(Guid wabaId, Guid templateId)
    {
        var response = await Sender.Send(new GetWhatsAppTemplateQuery
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId
        });
        return Ok(response);
    }

    /// <summary>Update template content (name/language unchanged).</summary>
    [HttpPut("{wabaId:guid}/templates/{templateId:guid}")]
    [OpenApiOperation("Update WhatsApp message template for WABA")]
    public async Task<IActionResult> UpdateTemplateAsync(
        Guid wabaId,
        Guid templateId,
        [FromBody] UpdateWhatsAppTemplateRequest request)
    {
        var response = await Sender.Send(new UpdateWhatsAppTemplateCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            Template = request
        });
        return Ok(response);
    }
}
