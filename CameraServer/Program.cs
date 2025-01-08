using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
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
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;

    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// TODO: add serilog with the compact json logger.
app.MapPost(
    "/SetCamera",
    async (HttpRequest request) =>
    {
        Console.WriteLine("/SetCamera endpoint hit");
        SetCameraMessage? message = null;

        try
        {
            message = await request.ReadFromJsonAsync<SetCameraMessage>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"/SetCamera Failed to parse message json");
            Console.WriteLine(ex.ToString());
            request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Results.BadRequest("failed to parse message json");
        }

        if (message is null)
        {
            request.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            // really this should never happen unless something is fucked
            return Results.BadRequest(
                "we parsed the message but it came out null. this shouldnt ever happen"
            );
        }
        Console.WriteLine($"/SetCamera received a valid message");

        var cam = Camera
            .GetCameras()
            .FirstOrDefault(c =>
                string.Equals(c.Name, message.Name, StringComparison.InvariantCultureIgnoreCase)
            );

        if (cam is null)
        {
            Console.WriteLine($"failed to find camera named {message.Name}");
            return Results.BadRequest($"failed to find camera named {message.Name} ");
        }

        if (message.SetToDefault is not null)
        {
            if (Convert.ToBoolean(message.SetToDefault.Value))
            {
                Camera.SetAllToDefault(cam);
            }

            Console.WriteLine($"Camera: {message.Name} has been reset to default settings");
            return Results.Ok($"Camera: {message.Name} has been reset to default settings");
        }

        Camera.ApplyMesage(cam, message);

        return Results.Ok($"Camera: {message.Name} has been set to the requested settings");
    }
);

app.MapGet(
    "/GetCamera/{cameraName}",
    (string cameraName) =>
    {
        if (string.IsNullOrWhiteSpace(cameraName))
        {
            Console.WriteLine($"/GetCamera , cameraName not provided");

            return Results.BadRequest("cameraName not provided");
        }
	
        var camera = Camera.GetCameras().FirstOrDefault(c => string.Equals(cameraName, c.Name,StringComparison.CurrentCultureIgnoreCase));
	

        if (camera is null)
        {
            Console.WriteLine($"/GetCamera , camera with name:{cameraName}, was not found!");
            return Results.BadRequest($"camera with name:{cameraName}, was not found!");
        }

        var settings = Camera.GetCameraSettings(camera);
        var jsonOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,

        };

      //  var json = JsonSerializer.Serialize(settings, jsonOptions);
        Console.WriteLine($"/GetCamera/{cameraName} found settings");

        return Results.Json(settings, statusCode: StatusCodes.Status200OK);
    }
);

await app.RunAsync();
