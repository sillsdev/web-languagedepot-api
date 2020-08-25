import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';
import { UsersService } from '../services/users.service';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ProjectsService } from '../services/projects.service';
import { Project } from '../models/project.model';
import { JsonApiService } from '../services/json-api.service';

@Component({
  selector: 'app-single-user',
  templateUrl: './single-user.component.html',
  styleUrls: ['./single-user.component.scss']
})
export class SingleUserComponent implements OnInit {
  user = new BehaviorSubject<User>(null);
  projects: Project[];

  constructor(private route: ActivatedRoute, private jsonApi: JsonApiService,
              private users: UsersService, private projectsService: ProjectsService) { }

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

  editUser(): void {
    const body = {
      login: { username: 'rhood', password: 'y' },
      // removeUser: 'rhood',
      // remove: [{username: 'rhood', role: 'Contributor'}],
      add: [{username: 'rhood', role: 'Contributor'}],
    };
    this.jsonApi.addRemoveUserExp<any>(body).subscribe();
  }

  createUser(): void {
    const body = {
      login: { username: 'x', password: 'y' },
      username: 'x',
      password: 'y',
      mustChangePassword: false,
      firstName: 'Joe',
      lastNames: 'Test',
      // language: (not provided, let's see what happens)
      emailAddresses: 'joe_test@example.com'
    };
    this.jsonApi.createUserExp<any>(body).subscribe();
  }
}
