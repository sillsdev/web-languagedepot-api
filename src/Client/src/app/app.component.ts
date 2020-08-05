import { Component, ViewChild, OnInit } from '@angular/core';
import { JsonApiService } from './services/json-api.service';
import { retry } from 'rxjs/operators';
import { PageEvent, MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.sass']
})
export class AppComponent implements OnInit {
  title = 'SafeNg';

  numbersData: string[];
  numbersLen: number;
  pageEvent: PageEvent;
  dataSource: MatTableDataSource<string>;
  @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;

  constructor(private readonly jsonApi: JsonApiService ) {
    this.dataSource = new MatTableDataSource();
  }
  ngOnInit(): void {
    this.dataSource.paginator = this.paginator;
  }

  hello(): void {
    const result = this.jsonApi.call<string>('/api/hello');
    result.subscribe(res => console.log('Server-sent hello message was', res));
  }
  fail(): void {
    const result = this.jsonApi.call<string>('/api/fail');
    result.subscribe({
      next: res => { console.log('Server-sent result was', res); },
      error: err => { console.log('Server-sent error message was', err); },
      complete: () => { console.log('fail() observable is complete'); }
    });
  }
  roles(): void {
    const result = this.jsonApi.call<string[]>('/api/roles');
    result
      .pipe(retry(3))
      .subscribe(res => { this.dataSource.data = res; });
  }
}
