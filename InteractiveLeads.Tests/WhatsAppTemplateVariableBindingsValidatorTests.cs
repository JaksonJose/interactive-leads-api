using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

namespace InteractiveLeads.Tests;

public sealed class WhatsAppTemplateVariableBindingsValidatorTests
{
    [Fact]
    public void Validate_HeaderAndBodySlots_RequiresMatchingBindings()
    {
        var dto = new WhatsAppTemplateDetailDto
        {
            HeaderText = "Hi {{1}}",
            Body = "Order {{1}} for {{2}}"
        };

        var bindings = new[]
        {
            new WhatsAppTemplateVariableBindingDto { Slot = 1, Source = "contact.name", Section = "header" },
            new WhatsAppTemplateVariableBindingDto { Slot = 1, Source = "company.name", Section = "body" },
            new WhatsAppTemplateVariableBindingDto { Slot = 2, Source = "contact.email", Section = "body" }
        };

        Assert.Null(WhatsAppTemplateVariableBindingsValidator.Validate(dto, bindings));
    }

    [Fact]
    public void Validate_WrongCount_ReturnsError()
    {
        var dto = new WhatsAppTemplateDetailDto { Body = "A {{1}}" };
        var bindings = Array.Empty<WhatsAppTemplateVariableBindingDto>();
        var err = WhatsAppTemplateVariableBindingsValidator.Validate(dto, bindings);
        Assert.NotNull(err);
    }

    [Fact]
    public void JsonMerger_MetaSync_PreservesSourceAndSetsBindings()
    {
        var json = /* lang=json,strict */ """
            {"source":"meta_template_sync","components":[{"type":"BODY","text":"Hello {{1}}"}]}
            """;
        var bindings = new[]
        {
            new WhatsAppTemplateVariableBindingDto { Slot = 1, Source = "contact.name", Section = "body", Example = "x" }
        };

        var mergeErr = WhatsAppTemplateVariableBindingsJsonMerger.TryMerge(json, bindings, out var next);
        Assert.Null(mergeErr);
        Assert.Contains("meta_template_sync", next, StringComparison.Ordinal);
        Assert.Contains("variableBindings", next, StringComparison.Ordinal);
        Assert.Contains("\"slot\":1", next, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonMerger_PersistedShape_RoundTripsVariableBindings()
    {
        var json = /* lang=json,strict */ """
            {"schemaVersion":1,"name":"n","language":"pt_BR","category":"MARKETING","body":"{{1}}","variableBindings":[]}
            """;
        var bindings = new[]
        {
            new WhatsAppTemplateVariableBindingDto { Slot = 1, Source = "user.display_name", Section = "body" }
        };

        Assert.Null(WhatsAppTemplateVariableBindingsJsonMerger.TryMerge(json, bindings, out var next));
        Assert.Contains("\"source\":\"user.display_name\"", next, StringComparison.Ordinal);
    }
}
