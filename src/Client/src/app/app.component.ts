import { Component, ViewChild, OnInit } from '@angular/core';
import { JsonApiService } from './services/json-api.service';
import { retry, map, tap } from 'rxjs/operators';
import { PageEvent, MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

import { Observable } from 'rxjs';
import { UsersService } from './services/users.service';
import { ProjectsService } from './services/projects.service';
import { Project } from './models/project.model';
import { Router } from '@angular/router';

type Table = 'Users' | 'Projects' | 'Roles';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'SafeNg';

  pageEvent: PageEvent;
  dataSource: MatTableDataSource<any>;
  @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;

  active: Table = 'Roles';

  columns: any;

  constructor(private readonly jsonApi: JsonApiService,
              private readonly usersService: UsersService,
              private readonly projectsService: ProjectsService,
              private readonly router: Router) {
    this.dataSource = new MatTableDataSource();
  }
  ngOnInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  private populateData<T>(result: Observable<T[]>, whichTable: Table): void {
    result
      .pipe(
        retry(3),
        )
      .subscribe(res => { this.active = whichTable; this.dataSource.data = res; });
  }

  itemSelected(item: any): void {
    if (this.active === 'Projects') {
      const proj = item as Project;
      this.router.navigateByUrl(`/admin/projects/${proj.code}`);
    }
  }

  users(): void {
    const result = this.usersService.getUsers();
    this.columns = {
      firstName: 'First Name',
      lastName: 'Last Name',
      email: 'Email',
      username: 'Username',
      language: 'Lang Tag'
    };
    this.populateData(result, 'Users');
  }
  projects(): void {
    const result = this.projectsService.getProjects().pipe(
      map(projects => projects.map(proj => ({...proj, get memberCount(): number { return proj.membership.length; }}))),
    );
    this.columns = {
      code: 'Project Code',
      description: 'Description',
      name: 'Project Name',
      memberCount: 'Member Count'
    };
    this.populateData(result, 'Projects');
  }
  roles(): void {
    const result = this.jsonApi.call<string[][]>('/api/roles');
    this.columns = ['Num', 'Role'];
    this.populateData(result, 'Roles');
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
    this.jsonApi.createUserExp<any>(body).pipe(
      tap(console.log)
    ).subscribe();
  }
  editUser(): void {
    const body = {
      login: { username: 'rhood', password: 'y' },
      // removeUser: 'rhood',
      // remove: [{username: 'rhood', role: 'Contributor'}],
      add: [{username: 'rhood', role: 'Contributor'}],
    };
    this.jsonApi.addRemoveUserExp<any>(body).pipe(
      tap(console.log)
    ).subscribe();
  }
  editUserSample(): void {
    this.jsonApi.addRemoveUserExpSample<any>().pipe(
      tap(console.log)
    ).subscribe();
  }

  projectExists(): void {
    this.jsonApi.projectExists('test-ws-1-flex').subscribe(console.log);
  }

  getProject(): void {
    this.jsonApi.getProject('test-ws-1-flex').subscribe(console.log);
  }

  projectDoesNotExist(): void {
    this.jsonApi.projectExists('no-such-project').subscribe(console.log);
  }
}
