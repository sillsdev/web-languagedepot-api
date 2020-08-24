import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JsonApiService } from './json-api.service';
import { Project, toProject, ApiProject } from '../models/project.model';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {

constructor(private readonly jsonApi: JsonApiService) { }

  public getProjects(): Observable<Project[]> {
    return this.jsonApi.call<ApiProject[]>('/api/projects').pipe(
      map(apiProjects => apiProjects.map(proj => toProject(proj)))
    );
  }

  public getProject(projectCode: string): Observable<Project> {
    return this.jsonApi.call<ApiProject>(`/api/projects/${projectCode}`).pipe(
      map(proj => toProject(proj))
    );
  }
}
