using System.Text.Json;
using System.Text.Json.Serialization;
using AForge.Video.DirectShow;
using CameraServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://localhost:5555");
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition =
        JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;

    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
var app = builder.Build();

app.MapPost(
    "/SetCamera",
    async (HttpRequest request) =>
    {
        Console.WriteLine("/SetCamera endpoint hit");
        var message = await request.ReadFromJsonAsync<SetCameraMessage>();

        if (message is null)
        
        Console.WriteLine($"/SetCamera received a valid message");

        var cam = Camera
            .GetCameras()
            .FirstOrDefault(c =>
                string.Equals(c.Name, message.Name, StringComparison.InvariantCultureIgnoreCase)
            );

        if (cam is null)
        {
            request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (message.SetToDefault is not null)
        {
            if (Convert.ToBoolean(message.SetToDefault.Value))
            {
                Camera.SetAllToDefault(cam);
            }

            return;
        }

        Camera.ApplyMesage(cam, message);

        request.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
    }
);

await app.RunAsync();
