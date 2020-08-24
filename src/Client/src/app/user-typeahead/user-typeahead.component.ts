import { Component, OnInit, Input, Output, EventEmitter, ViewChild, AfterViewChecked, AfterViewInit, ElementRef } from '@angular/core';
import { UsersService } from '../services/users.service';
import { User } from '../models/user.model';
import { Observable, fromEvent } from 'rxjs';
import { tap, map, debounceTime, distinctUntilChanged, switchMap, catchError, retry } from 'rxjs/operators';

@Component({
  selector: 'app-user-typeahead',
  templateUrl: './user-typeahead.component.html',
  styleUrls: ['./user-typeahead.component.scss']
})
export class UserTypeaheadComponent implements AfterViewInit {
  @Output() foundUsers = new EventEmitter<User[]>();
  @ViewChild('input') input: ElementRef;

  keyboardInput: Observable<any>;

  constructor(private users: UsersService) {
    this.keyboardInput = new Observable<any>();
  }

  ngAfterViewInit(): void {
    console.log('Input:', this.input);
    fromEvent(this.input.nativeElement, 'input').pipe(
      map((e: KeyboardEvent) => (e.target as HTMLInputElement).value),
      debounceTime(100),
      distinctUntilChanged(),
      tap(console.log),
      switchMap(text => this.doTheSearch(text)),
      retry(),
    ).subscribe(users => this.foundUsers.emit(users));
  }

  ngOnInit(): void {
  }

  doTheSearch(searchText: string): Observable<User[]> {
    return this.users.searchUsers(searchText);
  }

}
