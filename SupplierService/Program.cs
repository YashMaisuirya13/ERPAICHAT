using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Shared.Events.Extensions;
using Shared.Events.Middleware;
using SupplierService.Data;
using SupplierService.Repositories;
using SupplierService.Repositories.Interfaces;
using SupplierService.Services;
using SupplierService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<SupplierDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SupplierDatabase")));

// Register Repositories
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

// Register Services
builder.Services.AddScoped<ISupplierService, SupplierService.Services.SupplierService>();

builder.Services.AddSwaggerWithJwt("Supplier Service API");

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
