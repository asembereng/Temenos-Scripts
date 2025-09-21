using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Hangfire;
using Hangfire.SqlServer;
using TemenosAlertManager.Infrastructure.Data;
using TemenosAlertManager.Infrastructure.Repositories;
using TemenosAlertManager.Infrastructure.Services;
using TemenosAlertManager.Core.Interfaces;
using TemenosAlertManager.Api.Security;
using TemenosAlertManager.Api.Services;
using TemenosAlertManager.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
builder.Services.AddDbContext<TemenosAlertContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Windows Authentication
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Configure authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OperatorOrAdmin", policy => policy.RequireRole("Operator", "Admin"));
    options.AddPolicy("AllUsers", policy => policy.RequireAuthenticatedUser());
});

// Register repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IPowerShellService, PowerShellService>();
builder.Services.AddScoped<IMonitoringJobService, MonitoringJobService>();

// SOD/EOD services
builder.Services.AddScoped<ITemenosOperationService, TemenosOperationService>();
builder.Services.AddScoped<IServiceManagementService, ServiceManagementService>();

// Phase 2: Advanced orchestration services
builder.Services.AddScoped<ISODOrchestrator, SODOrchestrator>();
builder.Services.AddScoped<IEODOrchestrator, EODOrchestrator>();
builder.Services.AddScoped<IDependencyManager, DependencyManager>();
builder.Services.AddSingleton<IOperationMonitor, OperationMonitor>();

// Phase 3: Advanced features services
builder.Services.AddScoped<IOperationScheduler, OperationScheduler>();
builder.Services.AddScoped<IPerformanceOptimizer, PerformanceOptimizer>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IDisasterRecoveryService, DisasterRecoveryService>();

// Phase 4: Testing and deployment services
builder.Services.AddScoped<ITestingService, TestingService>();
builder.Services.AddScoped<IPerformanceTestingService, PerformanceTestingService>();
builder.Services.AddScoped<ISecurityTestingService, SecurityTestingService>();
builder.Services.AddScoped<IProductionDeploymentService, ProductionDeploymentService>();
builder.Services.AddScoped<IProductionMonitoringService, ProductionMonitoringService>();
builder.Services.AddScoped<IQualityAssuranceService, QualityAssuranceService>();

// Register background services
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<MonitoringSchedulerService>();

// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Ensure database is created and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TemenosAlertContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Seed initial configuration if needed
    await SeedInitialData(context);
}

try
{
    Log.Information("Starting Temenos Alert Manager API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Seed initial configuration data
static async Task SeedInitialData(TemenosAlertContext context)
{
    // Seed default AD group mappings if they don't exist
    if (!await context.AuthConfigs.AnyAsync())
    {
        var defaultAuthConfigs = new[]
        {
            new TemenosAlertManager.Core.Entities.Configuration.AuthConfig
            {
                AdGroupName = "TEMENOS_ALERT_ADMINS",
                Role = TemenosAlertManager.Core.Enums.UserRole.Admin,
                Description = "Full administrative access to Temenos Alert Manager"
            },
            new TemenosAlertManager.Core.Entities.Configuration.AuthConfig
            {
                AdGroupName = "TEMENOS_ALERT_OPERATORS",
                Role = TemenosAlertManager.Core.Enums.UserRole.Operator,
                Description = "Operational access - can view, acknowledge alerts and run checks"
            },
            new TemenosAlertManager.Core.Entities.Configuration.AuthConfig
            {
                AdGroupName = "TEMENOS_ALERT_VIEWERS",
                Role = TemenosAlertManager.Core.Enums.UserRole.Viewer,
                Description = "Read-only access to dashboards and alerts"
            }
        };

        context.AuthConfigs.AddRange(defaultAuthConfigs);
        await context.SaveChangesAsync();
        
        Log.Information("Seeded default AD group configurations");
    }
}
