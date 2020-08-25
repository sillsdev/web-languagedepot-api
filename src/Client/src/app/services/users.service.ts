import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JsonApiService } from './json-api.service';
import { User } from '../models/user.model';
import { Project, ApiProject, toProject } from '../models/project.model';
import { map } from 'rxjs/operators';

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

  public getProjectsForUser(username: string): Observable<[Project, string][]> {
    return this.jsonApi.call<[ApiProject, string][]>(`/api/users/${username}/projects`).pipe(
      map(results => results.map(([proj, role]) => [toProject(proj), role]))
    );
  }
  // TODO: That's the second time we've needed that map(apiProjects => apiProjects.map(proj => toProject(proj))) pipe.
  // Move that to a utility service that both ProjectsService and UsersService can import, so we don't duplicate code
}
