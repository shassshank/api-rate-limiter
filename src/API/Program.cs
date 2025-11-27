using API.Middleware;
using API.Configuration;
using API.RateLimiting;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Register controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RateLimiter_";
});

// bindings
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection("RateLimiting"));
    
builder.Services.Configure<RateLimitPolicyOptions>(
    builder.Configuration.GetSection("RateLimitPolicies"));

builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection("ApiKeys"));

// Rate limiting strategies
builder.Services.AddSingleton<IRateLimitingStrategy, FixedWindowRateLimitingStrategy>();
builder.Services.AddSingleton<IRateLimitingStrategy, SlidingWindowRateLimitingStrategy>();
builder.Services.AddSingleton<IRateLimitingStrategy, TokenBucketRateLimitingStrategy>();

// API
builder.Services.AddSingleton<IApiKeyRegistry, ApiKeyRegistry>();

// Identity resolver
builder.Services.AddSingleton<IRateLimitIdentityResolver, DefaultRateLimitIdentityResolver>();

// Strategy selector
builder.Services.AddSingleton<IRateLimitingStrategySelector, RateLimitingStrategySelector>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();