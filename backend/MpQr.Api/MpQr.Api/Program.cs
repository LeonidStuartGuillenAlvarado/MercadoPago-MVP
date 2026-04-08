using MpQr.Api.Hubs;
using MpQr.Api.Persistence;
using MpQr.Api.Security;
using MpQr.Api.Services.Interfaces;
using MpQr.Api.Services.MercadoPago;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ──────────────────────────────────────────────────────────────────────
// Restringido a los orígenes conocidos. Agregar más en AllowedOrigins (appsettings).
var allowedOrigins = builder.Configuration
    .GetSection("App:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── DI ───────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<StorePaymentRepository>();
builder.Services.AddScoped<IPaymentGateway, MercadoPagoCheckoutApiGateway>();
builder.Services.AddScoped<MercadoPagoSignatureValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FrontendPolicy");
app.UseAuthorization();

app.MapControllers();
app.MapHub<PaymentHub>("/paymentHub");

app.Run();
