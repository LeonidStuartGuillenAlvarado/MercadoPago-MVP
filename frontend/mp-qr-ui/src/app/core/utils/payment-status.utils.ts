export type PaymentStatus = 'idle' | 'pending' | 'approved' | 'rejected' | 'cancelled' | 'in_process';

export function normalizeStatus(value: string): PaymentStatus {
  const allowed: PaymentStatus[] = ['idle', 'pending', 'approved', 'rejected', 'cancelled', 'in_process'];
  return allowed.includes(value as PaymentStatus) ? (value as PaymentStatus) : 'idle';
}

export function isFinalStatus(status: PaymentStatus): boolean {
  return ['approved', 'rejected', 'cancelled'].includes(status);
}

/** Traduce el statusDetail de MercadoPago a texto legible. */
export function statusDetailLabel(detail: string | null | undefined): string {
  switch (detail) {
    case 'cc_rejected_insufficient_amount':    return 'Fondos insuficientes';
    case 'cc_rejected_bad_filled_security_code': return 'Código de seguridad incorrecto';
    case 'cc_rejected_bad_filled_date':        return 'Fecha de vencimiento incorrecta';
    case 'cc_rejected_call_for_authorize':     return 'Debe autorizar el pago con su banco';
    case 'cc_rejected_other_reason':           return 'Error general de la tarjeta';
    case 'accredited':                         return 'Pago acreditado';
    case 'pending_contingency':                return 'Pago en proceso (contingencia)';
    case 'pending_review_manual':              return 'En revisión manual';
    default:                                   return detail ?? '';
  }
}
