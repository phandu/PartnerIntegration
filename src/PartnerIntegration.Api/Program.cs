using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Http.Resilience;
using PartnerIntegration.Api.Services;
using PartnerIntegration.Api.Validation;
using Polly;
using PartnerIntegration.Api.Messaging;
using PartnerIntegration.Api.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PartnerTransactionRequestValidator>();
builder.Services
    .AddHttpClient<IPartnerVerificationService, PartnerVerificationService>(
        client =>
        {
            var baseUrl = builder.Configuration[
         "PartnerVerification:BaseUrl"];

            client.BaseAddress = new Uri(baseUrl!);
        })
    .AddResilienceHandler("partner-verification", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
    });
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.Run();
