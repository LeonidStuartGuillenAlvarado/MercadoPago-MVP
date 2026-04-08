import { Routes } from '@angular/router';
import { HomeComponent }       from './features/mvp/home/home.component';
import { WebShopComponent }    from './features/mvp/web-shop/web-shop.component';
import { WebCheckoutComponent} from './features/mvp/web-checkout/web-checkout.component';
import { StorePosComponent }   from './features/mvp/store-pos/store-pos.component';
import { ThankYouComponent }   from './features/mvp/thank-you/thank-you.component';

export const routes: Routes = [
  { path: '',          component: HomeComponent       },
  { path: 'web',       component: WebShopComponent    },
  { path: 'checkout',  component: WebCheckoutComponent},
  { path: 'store',     component: StorePosComponent   },
  { path: 'thank-you', component: ThankYouComponent   },
  { path: '**',        redirectTo: ''                  }
];
