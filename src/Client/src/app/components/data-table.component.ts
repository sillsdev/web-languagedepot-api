import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';

export interface ColumnDescription {
  [key: string]: string;
}

interface InternalColumnDescription {
  key: string;
  value: string;
}

@Component({
  selector: 'app-data-table',
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.scss']
})
export class DataTableComponent<T> implements OnInit {
  @ViewChild(MatPaginator, {static: true})
  paginator: MatPaginator;

  @Input()
  dataSource: MatTableDataSource<T>;

  @Input()
  pageSize: number;

  @Input()
  pageSizeOptions: number[];

  @Input()
  columns: string[] | ColumnDescription;

  columnKeys: string[];

  constructor() { }

  ngOnInit(): void {
    console.log('Initializing DataTable');
    this.dataSource.paginator = this.paginator;
    // this.pageSize ??= 10;  // Not available until Typescript 4.0
    // this.pageSizeOptions ??= [5, 10, 20, 50, 100];  // Not available until Typescript 4.0
    this.pageSize = this.pageSize ?? 10;
    this.pageSizeOptions = this.pageSizeOptions ?? [5, 10, 20, 50, 100];
    // if (this.columns == null) {
    //   throw new Error('"columns" attribute is required');
    // }
    this.columns = this.columns ?? [];
    this.columnKeys = Object.keys(this.columns);
  }

}
