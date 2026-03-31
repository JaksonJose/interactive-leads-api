using InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

namespace InteractiveLeads.Tests;

public sealed class WhatsAppTemplatePlaceholderCompilerTests
{
    [Fact]
    public void SemanticBody_CompilesToContiguousNumericSlots()
    {
        var header = (string?)null;
        var body = "Olá {{contact.name}}, aqui é {{user.display_name}} da {{company.name}}.";
        var he = (string?)null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out var bindings);

        Assert.Null(err);
        Assert.Equal("Olá {{1}}, aqui é {{2}} da {{3}}.", body);
        Assert.NotNull(bindings);
        Assert.Equal(3, bindings.Count);
        Assert.Equal("contact.name", bindings[0].Source);
        Assert.Equal(1, bindings[0].Slot);
        Assert.Equal("body", bindings[0].Section);
        Assert.Equal("user.display_name", bindings[1].Source);
        Assert.Equal("company.name", bindings[2].Source);
        Assert.NotNull(be);
        Assert.Equal(3, be!.Length);
    }

    [Fact]
    public void RepeatedSemanticToken_UsesSameSlot()
    {
        var header = (string?)null;
        var body = "Hi {{contact.name}}, bye {{contact.name}}.";
        var he = (string?)null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out var bindings);

        Assert.Null(err);
        Assert.Equal("Hi {{1}}, bye {{1}}.", body);
        Assert.Single(bindings!);
    }

    [Fact]
    public void MixedNumericAndSemantic_ReturnsError()
    {
        var header = (string?)null;
        var body = "{{1}} and {{contact.name}}";
        var he = (string?)null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.placeholders_mixed");
    }

    [Fact]
    public void NumericGap_ReturnsError()
    {
        var header = (string?)null;
        var body = "a {{1}} b {{3}}";
        var he = (string?)null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.placeholders_not_contiguous");
    }

    [Fact]
    public void UnknownSemantic_ReturnsError()
    {
        var header = (string?)null;
        var body = "{{not.a.real.field}}";
        var he = (string?)null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.unknown_variable");
    }

    [Fact]
    public void NumericLegacy_PassesThrough_AndNoBindings()
    {
        var header = (string?)null;
        var body = "Hello {{1}} and {{2}}.";
        string? he = "ex1";
        string[]? be = ["a", "b"];

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out var bindings);

        Assert.Null(err);
        Assert.Equal("Hello {{1}} and {{2}}.", body);
        Assert.Empty(bindings!);
        Assert.Equal("ex1", he);
        Assert.Equal(["a", "b"], be);
    }

    [Fact]
    public void ParseMetaSlots_ReturnsOrderedUnique()
    {
        var slots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots("x {{2}} y {{1}} {{2}}");
        Assert.Equal(new[] { 1, 2 }, slots);
    }

    [Fact]
    public void BodyPlaceholderAtStart_ReturnsError()
    {
        string? header = null;
        var body = "{{contact.name}} hello there.";
        string? he = null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.placeholder_body_edges");
    }

    [Fact]
    public void BodyPlaceholderAtEnd_ReturnsError()
    {
        string? header = null;
        var body = "Hello {{contact.name}}";
        string? he = null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.placeholder_body_edges");
    }

    [Fact]
    public void Body_VariableDensityTooHigh_ReturnsError()
    {
        string? header = null;
        var body = "a {{contact.name}} b {{company.name}} c {{user.email}} d";
        string? he = null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.variables_density_body");
    }

    [Fact]
    public void HeaderPlaceholderAtStart_ReturnsError()
    {
        string? header = "{{contact.name}} promo";
        var body = "Hello {{contact.name}} there.";
        string? he = null;
        string[]? be = null;

        var err = WhatsAppTemplatePlaceholderCompiler.TryCompileAndApply(
            ref header,
            ref body,
            ref he,
            ref be,
            null,
            null,
            out _,
            out _,
            out _);

        Assert.NotNull(err);
        Assert.Contains(err!.Messages, m => m.Code == "integrations.templates.placeholder_header_edges");
    }
}
