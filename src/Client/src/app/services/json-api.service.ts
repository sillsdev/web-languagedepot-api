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

  public post<T>(url: string, body: any): Observable<T> {
    return this.http
      .post<JsonResult<T>>(url, body)
      .pipe(map(res => { if (res.ok) { return res.data; } else { throw new Error(res.message); }}));
  }

  public createUserExp<T>(body: any): Observable<T> {
    return this.post<T>('/api/experimental/users', body);
  }
}
