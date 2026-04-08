import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface PaymentUpdateEvent {
  externalReference: string;
  status:            string;
  statusDetail?:     string | null;
}

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {

  private hubConnection?: signalR.HubConnection;
  // URL centralizada en environment — no más strings hardcodeados aquí
  private readonly hubUrl = `${environment.apiUrl}/paymentHub`;

  /**
   * Inicia la conexión y registra el callback para PaymentUpdated.
   * Se puede llamar múltiples veces; si ya existe una conexión activa, no crea otra.
   */
  startConnection(onUpdate: (data: PaymentUpdateEvent) => void): void {
    if (
      this.hubConnection &&
      this.hubConnection.state !== signalR.HubConnectionState.Disconnected
    ) {
      // Reconectar el handler sin crear nueva conexión
      this.hubConnection.off('PaymentUpdated');
      this.hubConnection.on('PaymentUpdated', onUpdate);
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(
        environment.production
          ? signalR.LogLevel.Warning
          : signalR.LogLevel.Information
      )
      .build();

    this.hubConnection.off('PaymentUpdated');
    this.hubConnection.on('PaymentUpdated', onUpdate);

    this.hubConnection
      .start()
      .then(() => console.log('[SignalR] Conectado'))
      .catch(err => console.error('[SignalR] Error al conectar:', err));
  }

  stopConnection(): void {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }
}
