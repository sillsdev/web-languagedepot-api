import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { JsonResult, JsonError } from '../models/json-api.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class JsonApiService {

  constructor(private readonly http: HttpClient) { }

  public call<T>(url: string): Observable<T> {
    return this.http
      .get<JsonResult<T>>(url)
      .pipe(map(res => { if (res.ok) { return res.data; } else { throw new Error(res.message); }}));
  }
}
