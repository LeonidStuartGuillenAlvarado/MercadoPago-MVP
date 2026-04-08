import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

interface Product {
  id:    number;
  name:  string;
  price: number;
  qty:   number;
}

@Component({
  standalone: true,
  imports: [CommonModule],
  templateUrl: './web-shop.component.html'
})
export class WebShopComponent {

  constructor(private router: Router) {}

  products = signal<Product[]>([
    { id: 1, name: 'Producto 1', price: 100, qty: 0 },
    { id: 2, name: 'Producto 2', price: 200, qty: 0 },
    { id: 3, name: 'Producto 3', price: 300, qty: 0 },
    { id: 4, name: 'Producto 4', price: 400, qty: 0 },
    { id: 5, name: 'Producto 5', price: 500, qty: 0 },
  ]);

  increase(p: Product) {
    p.qty++;
    this.products.update(v => [...v]);
  }

  decrease(p: Product) {
    if (p.qty > 0) p.qty--;
    this.products.update(v => [...v]);
  }

  totalItems()  { return this.products().reduce((a, b) => a + b.qty, 0); }
  totalAmount() { return this.products().reduce((a, b) => a + b.qty * b.price, 0); }

  continue() {
    localStorage.setItem('cart', JSON.stringify(this.products()));
    this.router.navigate(['/checkout']);
  }
}
