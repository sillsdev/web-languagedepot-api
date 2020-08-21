import { Component, ViewChild, OnInit } from '@angular/core';
import { JsonApiService } from './services/json-api.service';
import { retry, map, tap } from 'rxjs/operators';
import { PageEvent, MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

import { Observable } from 'rxjs';

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

  constructor(private readonly jsonApi: JsonApiService ) {
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
      .subscribe(res => { console.log('Got', res); this.active = whichTable; this.dataSource.data = res; });
  }

  users(): void {
    const result = this.jsonApi.call<object[]>('/api/users');
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
    const result = this.jsonApi.call<object[]>('/api/projects');
    this.columns = {
      code: 'Project Code',
      description: 'Description',
      name: 'Project Name'
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
