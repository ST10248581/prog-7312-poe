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
    });
builder.Services.AddOpenApi();

// Register application layers.
builder.Services.AddScoped<ITestRepository, TestRepository>();
builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddScoped<ISensorProfileRepository, SensorProfileRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IEngagementRepository, EngagementRepository>();

builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IEngagementService, EngagementService>();

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
builder.Services.AddSingleton<IDataSeeder, DataSeeder>();

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
