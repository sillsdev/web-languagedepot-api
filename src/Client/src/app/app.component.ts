import { Component, ViewChild, OnInit } from '@angular/core';
import { IdAndName } from './models/id-and-name.model';
import { JsonApiService } from './services/json-api.service';
import { retry, map } from 'rxjs/operators';
import { PageEvent, MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

import { ColumnDescription } from './components/data-table.component';
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
    this.populateData(result, 'Users');
  }
  projects(): void {
    const result = this.jsonApi.call<object[]>('/api/project');
    this.populateData(result, 'Projects');
  }
  roles(): void {
    const result = this.jsonApi.call<string[][]>('/api/roles');
    this.populateData(result, 'Roles');
  }
}
