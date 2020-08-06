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
  columnDescription: ColumnDescription;

  colDescs = new Array<InternalColumnDescription>();
  columns = new Array<string>();

  constructor() { }

  ngOnInit(): void {
    console.log('Initializing DataTable');
    this.dataSource.paginator = this.paginator;
    // this.pageSize ??= 10;  // Not available until Typescript 4.0
    // this.pageSizeOptions ??= [5, 10, 20, 50, 100];  // Not available until Typescript 4.0
    this.pageSize = this.pageSize ?? 10;
    this.pageSizeOptions = this.pageSizeOptions ?? [5, 10, 20, 50, 100];
    // tslint:disable-next-line: forin
    for (const key in this.columnDescription) {
      const value = this.columnDescription[key];
      this.colDescs.push({key, value});
      this.columns.push(key);
    }
  }

}
