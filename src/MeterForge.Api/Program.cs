using MeterForge.Api.Catalog;
using MeterForge.Api.Errors;
using MeterForge.Api.Pricing;
using MeterForge.Api.Subscriptions;
using MeterForge.Application.Catalog;
using MeterForge.Application.Subscriptions;
using MeterForge.Infrastructure.Catalog;
using MeterForge.Infrastructure.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<SubscriptionService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapProductEndpoints();
app.MapPricingEndpoints();
app.MapSubscriptionEndpoints();

app.Run();

public partial class Program;
