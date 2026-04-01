using InteractiveLeads.Api.Controllers.Base;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Commands;
using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts.Queries;
using InteractiveLeads.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace InteractiveLeads.Api.Controllers.Crm;

/// <summary>CRM WhatsApp Business Account (WABA): templates are scoped per WABA, not per phone number integration.</summary>
/// <remarks>
/// Mutations stay Owner/Manager. Template <b>read</b> (list + get by id) also allows <c>Agent</c> for inbox "Send template".
/// </remarks>
[Authorize]
public sealed class WhatsAppBusinessAccountsController : BaseApiController
{
    /// <summary>All WABAs for the company (including accounts with no phone numbers yet).</summary>
    [HttpGet]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("List WhatsApp Business Accounts for company")]
    public async Task<IActionResult> ListAsync()
    {
        var response = await Sender.Send(new ListWhatsAppBusinessAccountsQuery());
        return Ok(response);
    }

    /// <summary>Register a WABA (Meta id) before adding phone number integrations.</summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Create WhatsApp Business Account")]
    public async Task<IActionResult> CreateAccountAsync([FromBody] CreateWhatsAppBusinessAccountRequest request)
    {
        var response = await Sender.Send(new CreateWhatsAppBusinessAccountCommand { Account = request });
        return Ok(response);
    }

    /// <summary>Get a WABA linked to the current company (for labels / breadcrumbs).</summary>
    [HttpGet("{wabaId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Get WhatsApp Business Account by id")]
    public async Task<IActionResult> GetAsync(Guid wabaId)
    {
        var response = await Sender.Send(new GetWhatsAppBusinessAccountQuery { WhatsAppBusinessAccountId = wabaId });
        return Ok(response);
    }

    /// <summary>Get one template (metadata + body/header/footer/buttons hydrated from <c>ComponentsJson</c>).</summary>
    [HttpGet("{wabaId:guid}/templates/{templateId:guid}")]
    [Authorize(Roles = "Owner,Manager,Agent")]
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

    /// <summary>List cached message templates for the WABA (<see cref="InquiryRequest"/> body, same as <c>AdminTenants/list</c>).</summary>
    [HttpPost("{wabaId:guid}/templates/list")]
    [Authorize(Roles = "Owner,Manager,Agent")]
    [OpenApiOperation("List WhatsApp message templates for WABA (inquiry)")]
    public async Task<IActionResult> ListTemplatesInquiryAsync(Guid wabaId, [FromBody] InquiryRequest? inquiry)
    {
        var response = await Sender.Send(new ListWhatsAppTemplatesQuery
        {
            WhatsAppBusinessAccountId = wabaId,
            Pagination = inquiry ?? new InquiryRequest()
        });
        return Ok(response);
    }

    /// <summary>Queue a full template list sync from Meta (<c>template_synced</c> on <c>interactive-template-outbound</c> when RabbitMQ is enabled).</summary>
    [HttpPost("{wabaId:guid}/templates/sync")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Request WhatsApp templates sync (RabbitMQ)")]
    public async Task<IActionResult> RequestTemplatesSyncAsync(Guid wabaId)
    {
        var response = await Sender.Send(new RequestWhatsAppTemplatesSyncCommand
        {
            WhatsAppBusinessAccountId = wabaId
        });
        return Ok(response);
    }

    /// <summary>Submit a new template (queued to <c>interactive-template-outbound</c> when RabbitMQ is enabled).</summary>
    [HttpPost("{wabaId:guid}/templates")]
    [Authorize(Roles = "Owner,Manager")]
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

    /// <summary>
    /// Update template content on Meta (<c>update_template</c> on outbound queue when RabbitMQ is enabled). Requires an existing Meta template id.
    /// </summary>
    [HttpPut("{wabaId:guid}/templates/{templateId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update WhatsApp message template for WABA")]
    public async Task<IActionResult> UpdateTemplateAsync(
        Guid wabaId,
        Guid templateId,
        [FromBody] CreateWhatsAppTemplateRequest request)
    {
        var response = await Sender.Send(new UpdateWhatsAppTemplateCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            Template = request
        });
        return Ok(response);
    }

    /// <summary>
    /// Merge CRM semantic bindings for Meta <c>{{1}}..{{n}}</c> placeholders (synced or legacy templates). Does not re-submit the template to Meta.
    /// </summary>
    [HttpPatch("{wabaId:guid}/templates/{templateId:guid}/variable-bindings")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Update WhatsApp template variable bindings (CRM only)")]
    public async Task<IActionResult> UpdateTemplateVariableBindingsAsync(
        Guid wabaId,
        Guid templateId,
        [FromBody] UpdateWhatsAppTemplateVariableBindingsRequest request)
    {
        var response = await Sender.Send(new UpdateWhatsAppTemplateVariableBindingsCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            Request = request ?? new UpdateWhatsAppTemplateVariableBindingsRequest()
        });
        return Ok(response);
    }

    /// <summary>Delete a template (body must include the template name for Meta); queued as <c>delete_template</c> when RabbitMQ is enabled.</summary>
    [HttpDelete("{wabaId:guid}/templates/{templateId:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Delete WhatsApp message template for WABA")]
    public async Task<IActionResult> DeleteTemplateAsync(
        Guid wabaId,
        Guid templateId,
        [FromBody] DeleteWhatsAppTemplateRequest request)
    {
        var response = await Sender.Send(new DeleteWhatsAppTemplateCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            Request = request
        });
        return Ok(response);
    }

    /// <summary>Disable a template (CRM only). Disabled templates remain visible but cannot be used for messaging.</summary>
    [HttpPost("{wabaId:guid}/templates/{templateId:guid}/disable")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Disable WhatsApp message template (CRM only)")]
    public async Task<IActionResult> DisableTemplateAsync(Guid wabaId, Guid templateId)
    {
        var response = await Sender.Send(new SetWhatsAppTemplateDisabledCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            IsDisabled = true
        });
        return Ok(response);
    }

    /// <summary>Enable a template (CRM only). Only allowed when template is not pending delete.</summary>
    [HttpPost("{wabaId:guid}/templates/{templateId:guid}/enable")]
    [Authorize(Roles = "Owner,Manager")]
    [OpenApiOperation("Enable WhatsApp message template (CRM only)")]
    public async Task<IActionResult> EnableTemplateAsync(Guid wabaId, Guid templateId)
    {
        var response = await Sender.Send(new SetWhatsAppTemplateDisabledCommand
        {
            WhatsAppBusinessAccountId = wabaId,
            TemplateId = templateId,
            IsDisabled = false
        });
        return Ok(response);
    }
}
