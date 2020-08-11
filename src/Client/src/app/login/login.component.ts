import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  // userProfile$ = this.auth.userProfile$;
  // loggedIn$ = this.auth.loggedIn$;
  userProfile$ = this.auth.userProfile$.pipe(tap(res => { console.log('user profile pipe got', res); }));
  loggedIn$ = this.auth.loggedIn$.pipe(tap(res => { console.log('login service pipe got', res); }));

  constructor(readonly auth: AuthService) { }

  ngOnInit(): void {
  }

  login(): void {
    this.auth.login();
    // TODO: https://stackoverflow.com/questions/2587677/avoid-browser-popup-blockers suggests we should open a popup window here,
    // then pass it to login() as a parameter to loginWithPopup() to persuade Firefox to let our popup through
  }

  logout(): void {
    this.auth.logout();
  }

}
