import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

import { ICharge } from './charge';
import { Observable } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
  })

export class ChargesService {
    private url = 'https://teslacharging.azurewebsites.net/api/GetSavedCharges?code=XVQaB8lo129AaWoBZpuUQUK45sl1JRyTkJqC1/aZOuvS8CJ6f0WSgg==&vin=5YJ3F7EB9KF490943';
    private charges: any;
    constructor(private http: HttpClient) { }

    getData(): Observable<ICharge[]> {
        return this.http.get<ICharge[]>(this.url)
        .pipe(
            tap(data => console.log('All: ' + JSON.stringify(data)))
        )        
    };
}