using AIAgentService.AI;
using AIAgentService.Data;
using AIAgentService.Handlers;
using AIAgentService.Services;
using AIAgentService.Tools;
using AIAgentService.Tools.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Events.Extensions;
using Shared.Events.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<BearerTokenHandler>();

builder.Services.AddHttpClient<INemotronService, NemotronService>();
builder.Services.AddHttpClient<IClaudeService, ClaudeService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAIService, AIServiceFactory>();

// Register HttpClient factories for ERP services with BearerTokenHandler
builder.Services.AddHttpClient("InventoryService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:InventoryService"] ?? "http://localhost:5045");
}).AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient("SupplierService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:SupplierService"] ?? "http://localhost:5046");
}).AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient("ProcurementService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProcurementService"] ?? "http://localhost:5047");
}).AddHttpMessageHandler<BearerTokenHandler>();

// Register ERP Tools & Tool Registry
builder.Services.AddScoped<IERPTools, ERPTools>();
builder.Services.AddScoped<IERPToolRegistry, ERPToolRegistry>();

// Register Database Context & Chat History Service
builder.Services.AddDbContext<AIAgentDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AIAgentDatabase"));
});
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Initialize Database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AIAgentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("[AIAgentService] ERP_AIAgentDb verified/created successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[AIAgentService] Failed to initialize ERP_AIAgentDb database.");
    }
}

app.Run();
