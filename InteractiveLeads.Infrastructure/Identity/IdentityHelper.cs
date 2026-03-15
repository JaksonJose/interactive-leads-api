using Microsoft.AspNetCore.Identity;

namespace InteractiveLeads.Infrastructure.Identity
{
    internal static class IdentityHelper
    {
        internal static List<string> GetIdentityResultErrorDescriptions(IdentityResult identityResult)
        {
            var errorDescriptions = new List<string>();

            foreach (var error in identityResult.Errors)
            {
                errorDescriptions.Add(error.Description);
            }

            return errorDescriptions;
        }
    }
}
