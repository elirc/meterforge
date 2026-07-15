using MeterForge.Api.Catalog;
using MeterForge.Api.Errors;
using MeterForge.Application.Catalog;
using MeterForge.Infrastructure.Catalog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapProductEndpoints();

app.Run();

public partial class Program;
