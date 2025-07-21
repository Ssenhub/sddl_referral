namespace SddlReferral.Middlewares
{
    public class ContollerExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ContollerExceptionMiddleware> logger;
        private readonly IHostEnvironment env;

        public ContollerExceptionMiddleware(RequestDelegate next, ILogger<ContollerExceptionMiddleware> logger, IHostEnvironment env)
        {
            this.next = next;
            this.logger = logger;
            this.env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.next(context); // proceed to next middleware/controller
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unhandled exception occurred");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "Internal Server Error",
                    Detail = this.env.IsDevelopment() ? ex.ToString() : null
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
