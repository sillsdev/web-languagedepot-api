import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JsonApiService } from './json-api.service';
import { Project } from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {

constructor(private readonly jsonApi: JsonApiService) { }

  public getProjects(): Observable<Project[]> {
    return this.jsonApi.call('/api/project');
  }

  public getProject(projectCode: string): Observable<Project> {
    return this.jsonApi.call(`/api/project/${projectCode}`);
  }
}
