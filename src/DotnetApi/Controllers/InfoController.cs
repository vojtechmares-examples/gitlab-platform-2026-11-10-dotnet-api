using Microsoft.AspNetCore.Mvc;

namespace DotnetApi.Controllers;

/// <summary>Which build runs where: the first thing to look at after a deployment.</summary>
[ApiController]
[Route("api/[controller]")]
public class InfoController(IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    /// <summary>Application name, build (pipeline id), environment and runtime.</summary>
    [HttpGet]
    public object Get() => new
    {
        application = "dotnet-api",
        version = typeof(InfoController).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
        build = configuration["APP_BUILD"] ?? "local",
        environment = environment.EnvironmentName,
        host = Environment.MachineName,
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        time = DateTimeOffset.UtcNow,
    };
}
