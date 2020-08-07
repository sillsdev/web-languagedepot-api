import { Component, OnInit } from '@angular/core';
import { Project } from '../models/project.model';
import { ProjectsService } from '../services/projects.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-single-project',
  templateUrl: './single-project.component.html',
  styleUrls: ['./single-project.component.scss']
})
export class SingleProjectComponent implements OnInit {
  project: Project;

  constructor(private route: ActivatedRoute, private readonly projects: ProjectsService) { }

  ngOnInit(): void {
    const params = this.route.snapshot.paramMap;
    this.projects.getProject(params.get('id')).subscribe(project => this.project = project);
  }

}
