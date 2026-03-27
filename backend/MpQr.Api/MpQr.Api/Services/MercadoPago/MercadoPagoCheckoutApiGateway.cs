using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MpQr.Api.Dtos;
using MpQr.Api.Persistence;
using MpQr.Api.Services.Interfaces;

namespace MpQr.Api.Services.MercadoPago
{
    public class MercadoPagoCheckoutApiGateway : IPaymentGateway
    {
        private readonly PaymentRepository _repository;
        private readonly IConfiguration _config;
        private readonly StorePaymentRepository _storeRepository;

        public MercadoPagoCheckoutApiGateway(
            PaymentRepository repository,
            StorePaymentRepository storeRepository,
            IConfiguration config)
        {
            _repository = repository;
            _storeRepository = storeRepository;
            _config = config;

            MercadoPagoConfig.AccessToken = config["MercadoPago:AccessToken"];
        }

        //public async Task<CreatePaymentResponseDto> CreatePaymentAsync(decimal amount)
        //{
        //    var externalReference = Guid.NewGuid().ToString();

        //    var request = new PreferenceRequest
        //    {
        //        Items = new List<PreferenceItemRequest>
        //        {
        //            new PreferenceItemRequest
        //            {
        //                Title = "Pago QR",
        //                Quantity = 1,
        //                CurrencyId = "ARS",
        //                UnitPrice = amount
        //            }
        //        },
        //        ExternalReference = externalReference,
        //        NotificationUrl = $"{_config["App:BaseUrl"]}/api/payments/webhook",

        //        // ⏳ EXPIRACIÓN AUTOMÁTICA
        //        Expires = true,
        //        ExpirationDateTo = DateTime.UtcNow.AddMinutes(10)
        //    };

        //    var client = new PreferenceClient();
        //    var preference = await client.CreateAsync(request);

        //    await _repository.InsertAsync(new Models.Payment
        //    {
        //        ExternalReference = externalReference,
        //        Status = "pending",
        //        Amount = amount
        //    });

        //    return new CreatePaymentResponseDto
        //    {
        //        ExternalReference = externalReference,
        //        QrCode = preference.InitPoint, // 🔥 esto es la URL pagable
        //        Status = "pending"
        //    };
        //}

        public async Task<CreatePaymentResponseDto> CreatePaymentAsync(decimal amount, string mode = "web")
        {
            try 
            { 
                    var prefix = mode == "store" ? "STORE" : "WEB";
                    var externalReference = $"{prefix}-{Guid.NewGuid()}";

                    var request = new PreferenceRequest
                    {
                        Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = mode == "store" ? "Pago Tienda Física" : "Compra Web",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = amount
                    }
                },
                        ExternalReference = externalReference,
                        NotificationUrl = $"{_config["App:BaseUrl"]}/api/payments/webhook",
                        Expires = true,
                        ExpirationDateTo = DateTime.UtcNow.AddMinutes(10)
                    };

                    var client = new PreferenceClient();
                    var preference = await client.CreateAsync(request);

                    var checkoutUrl = preference.InitPoint
                          ?? preference.SandboxInitPoint;

                    if (mode == "store")
                    {
                        await _storeRepository.InsertAsync(new Models.StorePayment
                        {
                            ExternalReference = externalReference,
                            Status = "pending",
                            Amount = amount,
                            IsEnabled = true,
                            CheckoutUrl = checkoutUrl
                        });
                    }
                    else
                    {
                        await _repository.InsertAsync(new Models.Payment
                        {
                            ExternalReference = externalReference,
                            Status = "pending",
                            Amount = amount
                        });
                    }

                    return new CreatePaymentResponseDto
                    {
                        ExternalReference = externalReference,
                        QrCode = checkoutUrl,
                        Status = "pending"
                    };
                }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR MP:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<PaymentStatusResponseDto> GetStatusAsync(string externalReference)
        {
            var status = await _repository.GetStatusAsync(externalReference);

            return new PaymentStatusResponseDto
            {
                Status = status
            };
        }


        public async Task<CancelPaymentResponseDto> CancelAsync(string externalReference)
        {
            await _repository.UpdateStatusAsync(externalReference, "cancelled");

            return new CancelPaymentResponseDto
            {
                Status = "cancelled"
            };
        }

        //bloqueo duro en el gateway
        public async Task ProcessWebhookAsync(
            string externalReference,
            string status,
            string mercadoPagoPaymentId)
        {
            // 🔵 Si es STORE
            if (externalReference.StartsWith("STORE-"))
            {
                var storePayment = await _storeRepository
                    .GetByExternalReferenceAsync(externalReference);

                if (storePayment == null)
                    return;

                // 🔒 Bloqueo estados finales
                if (storePayment.Status == "approved" ||
                    storePayment.Status == "cancelled" ||
                    storePayment.Status == "rejected")
                {
                    return;
                }

                // Solo procesar approved
                if (status == "approved")
                {
                    await _storeRepository.UpdateStatusAndMpIdAsync(
                        externalReference,
                        "approved",
                        mercadoPagoPaymentId
                    );
                }

                return;
            }

            // 🔵 Si es WEB (lógica actual)
            var client = new PaymentClient();
            var PaymentMp = await client.GetAsync(long.Parse(mercadoPagoPaymentId));

            var statusDetail = PaymentMp.StatusDetail;

            var payment = await _repository
                .GetByExternalReferenceAsync(externalReference);

            if (payment == null)
                return;

            if (payment.Status == "approved" ||
                payment.Status == "cancelled" ||
                payment.Status == "rejected")
            {
                return;
            }

            if (payment.MercadoPagoPaymentId == mercadoPagoPaymentId &&
                payment.Status == status)
            {
                return;
            }

            await _repository.UpdateStatusAndMpIdAsync(
                externalReference,
                status,
                statusDetail,
                mercadoPagoPaymentId
            );
        }

    }
}
