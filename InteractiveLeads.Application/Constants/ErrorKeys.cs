namespace InteractiveLeads.Application.Constants
{
    /// <summary>
    /// Constants for error message keys used in internationalization (i18n).
    /// These keys correspond to translation files in the frontend application.
    /// </summary>
    public static class ErrorKeys
    {
        // Authentication & Authorization
        public const string AUTH_INVALID_CREDENTIALS = "auth.invalid_credentials";
        public const string AUTH_USER_NOT_ACTIVE = "auth.user_not_active";
        public const string AUTH_SUBSCRIPTION_EXPIRED = "auth.subscription_expired";
        public const string AUTH_SUBSCRIPTION_NOT_ACTIVE = "auth.subscription_not_active";
        public const string AUTH_INVALID_TOKEN = "auth.invalid_token";
        public const string AUTH_TOKEN_GENERATION_FAILED = "auth.token_generation_failed";
        public const string AUTH_AUTHENTICATION_FAILED = "auth.authentication_failed";
        public const string AUTH_AUTHENTICATION_NOT_SUCCESSFUL = "auth.authentication_not_successful";

        // Tenant Management
        public const string TENANT_NOT_FOUND = "tenant.not_found";
        public const string TENANT_CREATION_FAILED = "tenant.creation_failed";
        public const string TENANT_ALREADY_EXISTS = "tenant.already_exists";
        public const string TENANT_UPDATE_FAILED = "tenant.update_failed";
        public const string TENANT_DEACTIVATION_FAILED = "tenant.deactivation_failed";
        public const string TENANT_DOES_NOT_EXIST = "tenant.does_not_exist";

        // General Errors
        public const string GENERAL_SOMETHING_WENT_WRONG = "general.something_went_wrong";
        public const string GENERAL_VALIDATION_FAILED = "general.validation_failed";
        public const string GENERAL_RESOURCE_NOT_FOUND = "general.resource_not_found";
        public const string GENERAL_ACCESS_DENIED = "general.access_denied";
        public const string GENERAL_CONFLICT = "general.conflict";
        public const string GENERAL_UNAUTHORIZED = "general.unauthorized";
        public const string GENERAL_FORBIDDEN = "general.forbidden";

        // Identity System
        public const string IDENTITY_USER_CREATION_FAILED = "identity.user_creation_failed";
        public const string IDENTITY_ROLE_ASSIGNMENT_FAILED = "identity.role_assignment_failed";
        public const string IDENTITY_PERMISSION_DENIED = "identity.permission_denied";
    }
}
