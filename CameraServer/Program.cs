using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using AForge.Video.DirectShow;
using CameraServer;
using CameraServer.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

// TODO: add some options to whitelist camera names, making it impossible for webrequests to effect certian camera settings
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("settings.json").Build();

// TODO: add option to choose what interface and port to listen on
builder.WebHost.UseUrls(configuration["url"]);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;

    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

Camera.LogCameraNames();
var app = builder.Build();

if (!Directory.Exists(configuration["presetPath"]))
{
    Directory.CreateDirectory("./presets/");
}
string _presetPath = new FileInfo("./presets/").FullName;

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

        var camera = Camera
            .GetCameras()
            .FirstOrDefault(c =>
                string.Equals(cameraName, c.Name, StringComparison.CurrentCultureIgnoreCase)
            );

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
string PresetFileNameBuilder(string cameraName, string presetName)
{
    cameraName = cameraName.Replace(' ', '_');
    return $"{cameraName}_{presetName.GetFileSafeString()}.json";
}
app.MapGet(
    "/LoadPreset",
    async (string cameraName, string presetName) =>
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            return Results.BadRequest("preset name is invalid");
        }

        var camera = Camera.GetCameraByName(cameraName);
        if (camera is null)
        {
            return Results.BadRequest($"could not find camera with name:{cameraName}");
        }

        var path = Path.Combine(_presetPath, PresetFileNameBuilder(cameraName, presetName));
        if (!File.Exists(path))
        {
            return Results.BadRequest(
                $"Could not find preset named {presetName} for camera {cameraName}"
            );
        }

        var json = File.ReadAllText(path);
        try
        {
            var settings = JsonSerializer.Deserialize<SetCameraMessage>(json);
            if (settings is null)
            {
                return Results.BadRequest(
                    $"Failed to load the saved preset for camera: {cameraName}, preset:{presetName}"
                );
            }
            Console.WriteLine($"Applying preset: {presetName} to camera:{cameraName}");
            Camera.ApplyMesage(camera, settings);
        }
        catch (System.Exception)
        {
            return Results.BadRequest(
                $"Failed to load the saved preset for camera: {cameraName}, preset:{presetName}"
            );
        }
    }
);
app.MapGet(
    "/SavePreset",
    async (string cameraName, string presetName) =>
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            return Results.BadRequest("preset name is invalid");
        }

        var camera = Camera.GetCameraByName(cameraName);
        if (camera is null)
        {
            return Results.BadRequest($"could not find camera with name:{cameraName}");
        }

        var settings = Camera.GetCameraSettings(camera);

        var json = JsonSerializer.Serialize(settings);

        var path = Path.Combine(_presetPath, PresetFileNameBuilder(cameraName, presetName));
        Console.WriteLine($"saving to {path}");
        await File.WriteAllTextAsync(path, json);
        return Results.Ok();
    }
);

await app.RunAsync();
