namespace InteractiveLeads.Infrastructure.Configuration
{
    /// <summary>
    /// Settings for user invitation and account activation (e.g. frontend URL for activation link).
    /// </summary>
    public class ActivationSettings
    {
        public const string SectionName = "Activation";

        /// <summary>Base URL of the frontend application, used to build the activation link (e.g. https://app.example.com or http://localhost:4200).</summary>
        public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
    }
}
