import { Component, OnInit } from '@angular/core';

import { ICharge } from './charge';
import { ChargesService } from './charges.service';

@Component({
  selector: 'app-charge-list',
  templateUrl: './charge-list.component.html',
  styleUrls: ['./charge-list.component.css']
})
export class ChargeListComponent implements OnInit {
  pageTitle = 'Charge List';
  charges: ICharge[];

  constructor(private chargesService: ChargesService) { }

  ngOnInit(): void {
    this.chargesService.getData().subscribe({
      next: charges => {
        this.charges = charges.sort(function(a,b){
          if (a.date < b.date)    return 1;
          else if(a.date > b.date) return  -1;
          else return  0;
        });
        console.log("On page: " + this.charges);
      }
    });
  } 
}
