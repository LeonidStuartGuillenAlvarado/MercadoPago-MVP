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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

// DI
builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddScoped<PaymentRepository>();
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
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<PaymentHub>("/paymentHub");

app.Run();
