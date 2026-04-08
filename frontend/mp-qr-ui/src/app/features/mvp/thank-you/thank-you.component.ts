import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex flex-col items-center justify-center gap-6 bg-gray-50">
      <div class="text-6xl">🎉</div>
      <h1 class="text-3xl font-bold text-green-600">¡Gracias por su compra!</h1>
      <p class="text-gray-500 text-sm">Su pago fue procesado correctamente.</p>
      <button
        class="bg-blue-600 hover:bg-blue-700 transition text-white px-8 py-3 rounded-xl"
        (click)="goHome()">
        Volver al inicio
      </button>
    </div>
  `
})
export class ThankYouComponent {
  constructor(private router: Router) {}

  goHome() {
    localStorage.clear();
    this.router.navigate(['/']);
  }
}
