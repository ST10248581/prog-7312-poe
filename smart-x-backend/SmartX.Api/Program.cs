using System.Text.Json.Serialization;
using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Logic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Send enums to the frontend as names ("Warning") rather than numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Gateways mark a lost sample as "NaN" so the positions in a batch stay
        // aligned. JSON has no NaN literal, so accept the named form.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });
builder.Services.AddOpenApi();

// Register application layers.
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddScoped<ISensorProfileRepository, SensorProfileRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IEngagementRepository, EngagementRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepository>();

// The command stream is its own domain rather than part of the telemetry engine:
// it has no overlap with the ingest pipeline beyond the sensor profiles it targets.
builder.Services.AddScoped<ICommandService, CommandService>();

// One central service backs every domain interface, so the controllers keep
// depending on a narrow contract while the logic itself lives in one class.
builder.Services.AddScoped<SmartXTelemetryEngine>();
builder.Services.AddScoped<ISmartXTelemetryEngine>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<ISensorService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<ITelemetryService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<IAlertService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<IEngagementService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());

// In-memory data store and demo data seeding.
builder.Services.AddSingleton<SeedOptions>();
builder.Services.AddSingleton<ISmartXDataStore, SmartXDataStore>();
builder.Services.AddSingleton<ISensorProfileSeeder, SensorProfileSeeder>();
builder.Services.AddSingleton<ISensorThresholdSeeder, SensorThresholdSeeder>();
builder.Services.AddSingleton<ITelemetryReadingSeeder, TelemetryReadingSeeder>();
builder.Services.AddSingleton<IAlertSeeder, AlertSeeder>();
builder.Services.AddSingleton<ISensorAttachmentSeeder, SensorAttachmentSeeder>();
builder.Services.AddSingleton<IIngestionBatchSeeder, IngestionBatchSeeder>();
builder.Services.AddSingleton<IEngagementStateSeeder, EngagementStateSeeder>();
builder.Services.AddSingleton<ICommandSeeder, CommandSeeder>();
builder.Services.AddSingleton<IDataSeeder, DataSeeder>();

// Keeps the command stream moving: issues automated traffic and settles
// in-flight commands, so the dashboard shows live dispatch rather than a
// frozen log.
builder.Services.AddHostedService<CommandDispatchSimulator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Populate the in-memory store before the first request is served.
app.Services.GetRequiredService<IDataSeeder>().SeedAll();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.MapControllers();

app.Run();
