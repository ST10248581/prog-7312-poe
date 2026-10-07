using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using SmartX.Api.Configuration;
using SmartX.Api.Data;
using SmartX.Api.Data.Seeding;
using SmartX.Api.Logic;
using SmartX.Api.Logic.Attachments;

var builder = WebApplication.CreateBuilder(args);

// Typed configuration. Values come from appsettings.json and can be overridden
// per environment, by user-secrets or by environment variables
// (e.g. Frontend__AllowedOrigins__0, Attachments__EncryptionKey). Invalid
// settings stop the app at start-up rather than on the first request.
builder.Services.AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AttachmentOptions>()
    .Bind(builder.Configuration.GetSection(AttachmentOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var frontendOptions = builder.Configuration.GetSection(FrontendOptions.SectionName).Get<FrontendOptions>() ?? new FrontendOptions();
var attachmentOptions = builder.Configuration.GetSection(AttachmentOptions.SectionName).Get<AttachmentOptions>() ?? new AttachmentOptions();

// Multipart bodies are capped at the attachment limit plus a little room for
// the form's other fields, so an oversized upload is refused while it is being
// read rather than after it has been buffered.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = attachmentOptions.MaxFileSizeBytes + 64 * 1024;
});

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

// The command stream page has its own central engine. It is a singleton, not
// scoped: its queues, undo stack, registry, sensor logs and error-state sets are
// live state that has to survive between requests and be shared with the
// dispatch loop below.
builder.Services.AddSingleton<SmartXCommandEngine>();
builder.Services.AddSingleton<ISmartXCommandEngine>(provider => provider.GetRequiredService<SmartXCommandEngine>());

// One central service backs every domain interface, so the controllers keep
// depending on a narrow contract while the logic itself lives in one class.
builder.Services.AddScoped<SmartXTelemetryEngine>();
builder.Services.AddScoped<ISmartXTelemetryEngine>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<ISensorService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<ITelemetryService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<IAlertService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());
builder.Services.AddScoped<IEngagementService>(provider => provider.GetRequiredService<SmartXTelemetryEngine>());

// Encrypts attachments at rest. A singleton, so the key is loaded once.
builder.Services.AddSingleton<AttachmentCipher>();

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

// Emulated devices. They post telemetry to POST /api/commands/packets over
// HTTP, so simulated traffic reaches the intake exactly as a device's would.
builder.Services.AddHttpClient();
builder.Services.AddHostedService<DeviceTelemetrySimulator>();

// The CORS allow-list comes from Frontend:AllowedOrigins, so the dashboard can
// move host or port without a code change.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOptions.AllowedOrigins)
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
