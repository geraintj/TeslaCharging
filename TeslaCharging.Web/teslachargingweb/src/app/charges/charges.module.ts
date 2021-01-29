import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from "@angular/common/http";

import { ChargeListComponent } from './charge-list.component';

@NgModule({
  declarations: [
    ChargeListComponent
  ],
  imports: [
    CommonModule,
    HttpClientModule
  ]
})
export class ChargesModule {path: 'charges'; component: ChargeListComponent };
