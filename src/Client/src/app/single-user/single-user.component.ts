import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';
import { UsersService } from '../services/users.service';
import { Observable, ReplaySubject } from 'rxjs';
import { map, switchMap, distinctUntilChanged } from 'rxjs/operators';
import { ProjectsService } from '../services/projects.service';
import { Project } from '../models/project.model';
import { JsonApiService } from '../services/json-api.service';
import { NoticeService } from '../services/notice.service';

// TODO: Should be able to edit name, change password, edit email address (might need thinking about issues there)
// TODO: Projects search has checkboxes for joining multiple projects at once with the same role in each project being joined
// TODO: List projects this user belongs to, with role in those projects

@Component({
  selector: 'app-single-user',
  templateUrl: './single-user.component.html',
  styleUrls: ['./single-user.component.scss']
})
export class SingleUserComponent implements OnInit {
  user$ = new ReplaySubject<User>(1);
  user: User & {fullName: string};
  foundProjects: Project[];
  memberOf: [Project, string][];
  editMode = false;
  changePasswordMode = false;

  constructor(private route: ActivatedRoute, private jsonApi: JsonApiService,
              private users: UsersService, private projectsService: ProjectsService,
              private readonly notice: NoticeService) { }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => params.get('id')),
      switchMap(username => this.users.getUser(username)),
    ).subscribe(this.user$);
    // When user changes, get list of new user's projects
    this.user$.pipe(
      distinctUntilChanged()
    ).subscribe(newUser => {
      console.log('Looking up projects for', newUser.username);
      this.users.getProjectsForUser(newUser.username).subscribe(projects => this.memberOf = projects);
    });
    // Also keep a record of the current user in a non-observable for the template to use
    this.user$.subscribe(user => this.user = {...user, fullName: user.firstName + ' ' + user.lastName});
  }

  changePassword([oldPw, newPw]: [string, string]): void {
    this.changePasswordMode = false;
    const msg = oldPw == null
      ? `Password would be changed to ${newPw} (old password not required when logged in as an admin)`
      : `Password would be changed from ${oldPw} to ${newPw}`;
    // To show a brief notification that doesn't require interaction:
    // this.notice.show(msg);
    // To show a notification that requires clicking on the "Dismiss" button:
    this.notice.showMessageDialog(() => msg);
  }

  searchProjects(searchText: string): Observable<Project[]> {
    return this.projectsService.searchProjects(searchText);
  }

  onFoundProjects(projects: Project[]): void {
    this.foundProjects = projects;
  }

  editUser(): void {
    const body = {
      login: { username: 'rhood', password: 'y' },
      // removeUser: 'rhood',
      // remove: [{username: 'rhood', role: 'Contributor'}],
      add: [{username: 'rhood', role: 'Contributor'}],
    };
    this.jsonApi.addRemoveUserExp<any>(body).subscribe();
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
    this.jsonApi.createUserExp<any>(body).subscribe();
  }
}
