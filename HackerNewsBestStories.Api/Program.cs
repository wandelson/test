using System.Net.Http.Json;
using HackerNewsBestStories.Api.Application.Interfaces;
using HackerNewsBestStories.Api.Application.Services;
using HackerNewsBestStories.Api.Infrastructure.HackerNews;
using HackerNewsBestStories.Api.Infrastructure.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Sinks.Graylog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Graylog(new GraylogSinkOptions
        {
            HostnameOrAddress = context.Configuration["Graylog:Host"] ?? "localhost",
            Port = int.Parse(context.Configuration["Graylog:Port"] ?? "12201"),
            Facility = context.Configuration["Graylog:Facility"] ?? "HackerNewsBestStoriesApi"
        });
});

builder.Services.Configure<HackerNewsOptions>(builder.Configuration.GetSection("HackerNews"));
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HackerNews:BaseUrl"] ?? "https://hacker-news.firebaseio.com/v0/");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddScoped<IStoryService, StoryService>();

builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("HackerNewsBestStoriesApi"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
}

public partial class Program { }
