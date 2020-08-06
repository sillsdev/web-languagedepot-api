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
  columnDescription: ColumnDescription;

  colDescs = new Array<InternalColumnDescription>();
  columns = new Array<string>();

  constructor() { }

  ngOnInit(): void {
    console.log('Initializing DataTable');
    this.dataSource.paginator = this.paginator;
    // tslint:disable-next-line: forin
    for (const key in this.columnDescription) {
      const value = this.columnDescription[key];
      this.colDescs.push({key, value});
      this.columns.push(key);
    }
  }

}
