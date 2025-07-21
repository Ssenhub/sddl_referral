namespace SddlReferral.Utils
{
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    public static class Utility
    {
        public static string GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value ?? string.Empty;
        }
    }
}
