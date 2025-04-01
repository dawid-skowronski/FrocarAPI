using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Przechodzi do następnego middleware
        await _next(context);

        // Jeśli odpowiedź ma kod 401, zwróć niestandardowy komunikat
        if (context.Response.StatusCode == 401)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                message = "Musisz być zalogowany, aby uzyskać dostęp do tej funkcji."
            }));
        }
    }
}
