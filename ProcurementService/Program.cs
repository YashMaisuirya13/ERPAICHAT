using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProcurementService.Clients;
using ProcurementService.Clients.Interfaces;
using ProcurementService.Data;
using ProcurementService.Repositories;
using ProcurementService.Repositories.Interfaces;
using ProcurementService.Services;
using ProcurementService.Services.Interfaces;
using Shared.Events.Extensions;
using Shared.Events.Http;
using Shared.Events.Messaging;
using Shared.Events.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProcurementDatabase")));

// Register Event Bus (RabbitMQ)
builder.Services.AddRabbitMqEventBus();

// Register Repositories
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

// Register CorrelationIdDelegatingHandler
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpContextAccessor();

// Register Inter-Service HTTP Clients with resilience & timeouts
builder.Services.AddHttpClient<ISupplierClient, SupplierClient>(client =>
{
    var url = builder.Configuration["ServiceUrls:SupplierService"] ?? "http://localhost:5046";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    var url = builder.Configuration["ServiceUrls:InventoryService"] ?? "http://localhost:5045";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(5);
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// Register Services
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Procurement Service API",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
