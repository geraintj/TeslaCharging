import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams, HttpParameterCodec } from '@angular/common/http';
import { environment } from './../../environments/environment';

import { ICharge } from './charge';
import { Observable } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { env } from 'process';

@Injectable({
    providedIn: 'root'
  })

export class ChargesService {
    private url = environment.apiUrl;
    private code = environment.apiCode;
    
    private charges: any;
    constructor(private http: HttpClient) { }

    getData(): Observable<ICharge[]> {
        let httpParams = new HttpParams()
            .set('code', this.code);
        
        console.log('allweddau params ' + httpParams.keys.length);
        return this.http.get<ICharge[]>(this.url + '5YJ3F7EB9KF490943', { params: httpParams })
        .pipe(
            tap(data => console.log('All: ' + JSON.stringify(data)))
        )        
    };

    deleteCharge(id: string): Observable<ICharge> {
        let httpParams = new HttpParams()
        .set('code', this.code);
        
        var deleteUrl = encodeURI(this.url + '5YJ3F7EB9KF490943/' + id);
        return this.http.delete<ICharge>(deleteUrl, { params: httpParams });
    };
}