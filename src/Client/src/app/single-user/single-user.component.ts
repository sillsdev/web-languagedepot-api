import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';
import { UsersService } from '../services/users.service';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ProjectsService } from '../services/projects.service';
import { Project } from '../models/project.model';

@Component({
  selector: 'app-single-user',
  templateUrl: './single-user.component.html',
  styleUrls: ['./single-user.component.scss']
})
export class SingleUserComponent implements OnInit {
  user = new BehaviorSubject<User>(null);
  projects: Project[];

  constructor(private route: ActivatedRoute, private users: UsersService, private projectsService: ProjectsService) { }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => params.get('id')),
      // switchMap(this.users.getUser),  // WRONG! "this.jsonApi is undefined" because "this" is a SwitchMapSubscriber instance
      switchMap(username => this.users.getUser(username)),  // RIGHT! This makes "this" be a UsersService instance
    ).subscribe(this.user);
  }

  searchProjects(searchText: string): Observable<Project[]> {
    return this.projectsService.searchProjects(searchText);
  }

  foundProjects(projects: Project[]) {
    this.projects = projects;
  }
}
