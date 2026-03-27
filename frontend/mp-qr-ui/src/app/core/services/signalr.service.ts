import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;

  startConnection(onUpdate: (data: any) => void) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://postpalpebral-karrie-overhostilely.ngrok-free.dev/paymentHub', {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connected'))
      .catch((err) => console.error('SignalR Error:', err));
    


    this.hubConnection.off('PaymentUpdated'); // Asegura que no se dupliquen los manejadores
    this.hubConnection.on('PaymentUpdated', (data) => {
      console.log('Evento recibido:', data);
      onUpdate(data);
    });
  }

  stopConnection() {
    this.hubConnection?.stop();
  }
}
