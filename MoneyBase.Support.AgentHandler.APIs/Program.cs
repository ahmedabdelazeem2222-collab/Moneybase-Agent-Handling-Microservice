using MoneyBase.Support.Infrastructure.Extensions;
using Serilog;
using MoneyBase.Support.Infrastructure.AgentHub;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


#region Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
#endregion

#region IOC container - register services
builder.Services.AddMoneyBaseServices(builder.Configuration)
                .AddHostedServices();
#endregion

// Add CORS (for frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowCredentials()); // we opened all cors for demo purpose
});

// Register SignalR
builder.Services.AddSignalRCore();


var app = builder.Build();

app.MapGet("/", () => "Hello from MoneyBase.Support.AgentHandler.APIs");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseRouting();
app.UseCors("AllowAll");


// Map SignalR hub
app.MapHub<AgentHub>("/hubs/agent");

try
{
    Log.Information("Starting MoneyBase.Support.AgentHandler.APIs...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MoneyBase.Support.AgentHandler.APIs failed to start");
}
finally
{
    Log.CloseAndFlush();
}
