using MeterForge.Api.Adjustments;
using MeterForge.Api.Catalog;
using MeterForge.Api.Entitlements;
using MeterForge.Api.Errors;
using MeterForge.Api.Integrations;
using MeterForge.Api.Invoicing;
using MeterForge.Api.Payments;
using MeterForge.Api.Pricing;
using MeterForge.Api.Rating;
using MeterForge.Api.Reconciliation;
using MeterForge.Api.Subscriptions;
using MeterForge.Api.Usage;
using MeterForge.Application.Catalog;
using MeterForge.Application.Payments;
using MeterForge.Application.Subscriptions;
using MeterForge.Application.Usage;
using MeterForge.Infrastructure.Catalog;
using MeterForge.Infrastructure.Payments;
using MeterForge.Infrastructure.Subscriptions;
using MeterForge.Infrastructure.Usage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IPaymentProvider, SimulatedPaymentProvider>();
builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
builder.Services.AddSingleton<IUsageEventStore, InMemoryUsageEventStore>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SubscriptionAmendmentService>();
builder.Services.AddScoped<UsageIngestionService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapAdjustmentEndpoints();
app.MapEntitlementEndpoints();
app.MapInvoiceEndpoints();
app.MapIntegrationEndpoints();
app.MapPaymentEndpoints();
app.MapProductEndpoints();
app.MapPricingEndpoints();
app.MapRatingEndpoints();
app.MapReconciliationEndpoints();
app.MapSubscriptionEndpoints();
app.MapUsageEndpoints();

app.Run();

public partial class Program;
