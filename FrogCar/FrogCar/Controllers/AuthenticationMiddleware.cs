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

        var originalBodyStream = context.Response.Body;

        using (var memStream = new MemoryStream())
        {
            context.Response.Body = memStream;

            await _next(context);

            memStream.Position = 0;
            var responseBody = new StreamReader(memStream).ReadToEnd();

            context.Response.Body = originalBodyStream;

            if (context.Response.StatusCode == 401)
            {
                context.Response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(new
                {
                    message = "Musisz być zalogowany, aby uzyskać dostęp do tej funkcji."
                });
                await context.Response.WriteAsync(json);
            }
            else if (context.Response.StatusCode == 403)
            {
                context.Response.ContentType = "application/json";
                var json = JsonConvert.SerializeObject(new
                {
                    message = "Nie masz uprawnień do wykonania tej operacji."
                });
                await context.Response.WriteAsync(json);
            }
            else
            {
                memStream.Position = 0;
                await memStream.CopyToAsync(originalBodyStream);
            }
        }
    }
}