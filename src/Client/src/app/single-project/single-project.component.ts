import { Component, OnInit } from '@angular/core';
import { Project } from '../models/project.model';
import { ProjectsService } from '../services/projects.service';
import { RolesService } from '../services/roles.service';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';
import { Role } from '../models/role.model';

const fakeProject = {
  code: 'demo',
  name: 'Demo project',
  description: 'line1\nline2',
  membership: [{username: 'rmunn', role: 'Manager'}, {username: 'rhood', role: 'Contributor'}],
} as Project;

@Component({
  selector: 'app-single-project',
  templateUrl: './single-project.component.html',
  styleUrls: ['./single-project.component.scss']
})
export class SingleProjectComponent implements OnInit {
  project: Project;
  showAddPersonBox = false;
  usersFound: User[];
  userToAdd: User;
  roles: Role[];
  selectedRole: string;

  constructor(private route: ActivatedRoute,
              private readonly projects: ProjectsService,
              private readonly rolesService: RolesService)
  {
    this.rolesService.roles.subscribe(roles => this.roles = roles);
    this.rolesService.roles.subscribe(console.log);
  }

  ngOnInit(): void {
    const params = this.route.snapshot.paramMap;
    this.getProject(params.get('id'));
    // this.project = fakeProject;
  }

  getProject(code: string): void {
    this.projects.getProject(code).subscribe(project => this.project = project);
  }

  toggleAddPerson(): void {
    this.showAddPersonBox = !this.showAddPersonBox;
  }

  foundUsers(users: User[]): void {
    this.usersFound = users;
  }

  selectUser(user: User): void {
    this.userToAdd = user;
  }

  selectRole(role: string): void {
    this.selectedRole = role;
    console.log('selected role', role);
  }

  addMember(user: User, role: string): void {
    console.log('Will add', user, 'with role', role);
    this.projects.addUserWithRole(this.project.code, user, role).subscribe(() => {
      this.success(this.project.code, user, role);
      this.resetUserSearch();
      this.getProject(this.project.code);
    });
  }

  success(code: string, user: User, role: string): void {
    console.log('Successfully added', user, 'to', code, 'with role', role);
  }

  resetUserSearch(): void {
    this.userToAdd = undefined;
    this.selectedRole = undefined;
    this.usersFound = undefined;
  }
}
