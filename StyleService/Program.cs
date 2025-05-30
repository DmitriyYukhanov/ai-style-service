using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StyleService.Services;
using StyleService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Get Replicate token from environment
string replicateToken = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN")
                        ?? throw new InvalidOperationException("REPLICATE_API_TOKEN is not set");

// Configure services
builder.Services.AddHttpClient<IReplicateService, ReplicateService>(client =>
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", replicateToken);
});

builder.Services.AddScoped<IImageProcessor, ImageProcessor>();

var app = builder.Build();

// Configure endpoints
app.MapPost("/api/style", async (HttpRequest request, IImageProcessor imageProcessor, IReplicateService replicateService) =>
    await StyleEndpoints.HandleStyleTransfer(request, imageProcessor, replicateService));

app.MapPost("/api/style-flux", async (HttpRequest request, IImageProcessor imageProcessor, IReplicateService replicateService) =>
    await StyleEndpoints.HandleFluxStyleTransfer(request, imageProcessor, replicateService));

app.Run();