using Shared.Events.Extensions;
using Shared.Events.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers and JWT Authentication
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Map reverse proxy routes
app.MapReverseProxy();

app.MapGet("/", () => Results.Ok(new
{
    service = "ERP API Gateway",
    status = "Online",
    version = "1.0",
    routes = new[]
    {
        "/api/auth/token -> Auth Endpoint",
        "/api/inventory -> InventoryService (:5045)",
        "/api/products -> InventoryService (:5045)",
        "/api/suppliers -> SupplierService (:5046)",
        "/api/purchaseorders -> ProcurementService (:5047)"
    }
}));

app.Run();
