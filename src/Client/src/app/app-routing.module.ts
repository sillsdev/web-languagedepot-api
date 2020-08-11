import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { UsersComponent } from './users/users.component';
import { SingleUserComponent } from './single-user/single-user.component';
import { ProjectsComponent } from './projects/projects.component';
import { SingleProjectComponent } from './single-project/single-project.component';
import { AppComponent } from './app.component';

const routes: Routes = [
  { path: 'admin',
    children: [
      { path: 'users', component: UsersComponent },
      { path: 'users/:id', component: SingleUserComponent },
      { path: 'projects', component: ProjectsComponent },
      { path: 'projects/:id', component: SingleProjectComponent },
    ]
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
