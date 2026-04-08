export const environment = {
  production: false,
  // El frontend habla directamente con el backend local.
  // ngrok solo es necesario para que MercadoPago entregue webhooks desde internet
  // (se configura en appsettings.json → App:BaseUrl, no aquí).
  apiUrl: 'http://localhost:5251'
};
