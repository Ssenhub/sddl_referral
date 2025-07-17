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

        public static async Task<IActionResult> ExecuteAndThrow(Func<Task<IActionResult>> action, ILogger logger)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception occurred: {ex}");

                return new ObjectResult(ex.Message)
                {
                    StatusCode = 500
                };
            }
        }
    }
}
