import { Component, OnInit } from '@angular/core';
import { Project } from '../models/project.model';
import { ProjectsService } from '../services/projects.service';
import { ActivatedRoute } from '@angular/router';
import { User } from '../models/user.model';

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

  constructor(private route: ActivatedRoute, private readonly projects: ProjectsService) { }

  ngOnInit(): void {
    const params = this.route.snapshot.paramMap;
    // this.projects.getProject(params.get('id')).subscribe(project => this.project = project);
    this.project = fakeProject;
  }

  toggleAddPerson(): void {
    this.showAddPersonBox = !this.showAddPersonBox;
  }

  foundUsers(users: User[]): void {
    this.usersFound = users;
  }
}
