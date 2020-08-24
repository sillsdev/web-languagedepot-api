import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JsonApiService } from './json-api.service';
import { User } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UsersService {

constructor(private readonly jsonApi: JsonApiService) { }

  public getUsers(): Observable<User[]> {
    return this.jsonApi.call('/api/users');
  }

  public getUser(username: string): Observable<User> {
    return this.jsonApi.call(`/api/users/${username}`);
  }

  public createUser(body: any): Observable<User> {
    return this.jsonApi.createUserExp<User>(body);
  }

  public searchUsers(searchText: string): Observable<User[]> {
    return this.jsonApi.call(`/api/searchUsers/${searchText}`);
  }
}
