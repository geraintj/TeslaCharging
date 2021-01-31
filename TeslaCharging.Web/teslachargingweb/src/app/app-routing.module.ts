import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ChargeListComponent } from './charges/charge-list.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'charges',
    pathMatch: 'full'
  },
  {
    path: 'charges', 
    component: ChargeListComponent
  }];

@NgModule({
  imports: [RouterModule.forRoot(routes, { relativeLinkResolution: 'legacy' })],
  exports: [RouterModule]
})

export class AppRoutingModule { }

