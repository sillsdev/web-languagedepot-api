import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';
import { UsersService } from '../services/users.service';
import { BehaviorSubject } from 'rxjs';
import { map, switchMap, tap } from 'rxjs/operators';

@Component({
  selector: 'app-single-user',
  templateUrl: './single-user.component.html',
  styleUrls: ['./single-user.component.scss']
})
export class SingleUserComponent implements OnInit {
  user = new BehaviorSubject<User>(null);

  constructor(private route: ActivatedRoute, private users: UsersService) { }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => params.get('id')),
      // switchMap(this.users.getUser),  // WRONG! "this.jsonApi is undefined" because "this" is a SwitchMapSubscriber instance
      switchMap(username => this.users.getUser(username)),  // RIGHT! This makes "this" be a UsersService instance
    ).subscribe(this.user);
  }
}
