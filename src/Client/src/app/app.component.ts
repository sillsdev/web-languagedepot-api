import { Component, ViewChild, OnInit } from '@angular/core';
import { IdAndName } from './models/id-and-name.model';
import { JsonApiService } from './services/json-api.service';
import { retry, map } from 'rxjs/operators';
import { PageEvent, MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'SafeNg';

  pageEvent: PageEvent;
  dataSource: MatTableDataSource<IdAndName>;
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
      .pipe(
        retry(3),
        map(res => res.map(item => { return { id: item[0], name: item[1] } as IdAndName; }) )
        )
      .subscribe(res => { console.log("Got", res); this.dataSource.data = res; });
  }
}
