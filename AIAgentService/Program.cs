using AIAgentService.Tools;
using AIAgentService.Tools.Interfaces;
using Shared.Events.Extensions;
using Shared.Events.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();

// Register HttpClient factories for ERP services
builder.Services.AddHttpClient("InventoryService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:InventoryService"] ?? "http://localhost:5045");
});
builder.Services.AddHttpClient("SupplierService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:SupplierService"] ?? "http://localhost:5046");
});
builder.Services.AddHttpClient("ProcurementService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:ProcurementService"] ?? "http://localhost:5047");
});

// Register ERP Tools
builder.Services.AddScoped<IERPTools, ERPTools>();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
