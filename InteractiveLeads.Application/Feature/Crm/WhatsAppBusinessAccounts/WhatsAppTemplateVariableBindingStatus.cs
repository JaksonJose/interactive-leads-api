namespace InteractiveLeads.Application.Feature.Crm.WhatsAppBusinessAccounts;

internal static class WhatsAppTemplateVariableBindingStatus
{
    public static void Recalculate(WhatsAppTemplateDetailDto dto)
    {
        var headerSlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(dto.HeaderText);
        var bodySlots = WhatsAppTemplatePlaceholderCompiler.ParseMetaSlots(dto.Body);
        dto.VariableSlotCount = headerSlots.Count + bodySlots.Count;
        if (dto.VariableSlotCount == 0)
        {
            dto.VariableBindingsComplete = true;
            return;
        }

        var bindings = dto.VariableBindings ?? [];
        if (bindings.Length == 0)
        {
            dto.VariableBindingsComplete = false;
            return;
        }

        foreach (var s in headerSlots)
        {
            if (!bindings.Any(b =>
                    string.Equals(b.Section, "header", StringComparison.OrdinalIgnoreCase) && b.Slot == s))
            {
                dto.VariableBindingsComplete = false;
                return;
            }
        }

        foreach (var s in bodySlots)
        {
            if (!bindings.Any(b =>
                    string.Equals(b.Section, "body", StringComparison.OrdinalIgnoreCase) && b.Slot == s))
            {
                dto.VariableBindingsComplete = false;
                return;
            }
        }

        dto.VariableBindingsComplete = true;
    }
}
