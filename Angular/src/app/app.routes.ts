import { Routes } from '@angular/router';
import { CategoriesComponent } from './components/categories/categories.component';
import { adminGuard } from './services/admin.guard';

export const routes: Routes = [

  {
    path: 'login',
    loadComponent: () => import('./components/auth/login/login.component')
      .then(m => m.LoginComponent)
  },

  {
    path: 'register',
    loadComponent: () => import('./components/auth/register/register.component')
      .then(m => m.RegisterComponent)
  },

  {
  path: 'admin/categories',
  loadComponent: () => import('./components/categories/categories.component')
    .then(m => m.CategoriesComponent),
  canActivate: [adminGuard]
},



  {
  path: '',
  loadComponent: () =>
    import('./components/home/home.component').then(m => m.HomeComponent)
},

 {
    path: 'categories',
    loadComponent: () =>
      import('./components/public-categories/public-categories.component')
        .then(m => m.PublicCategoriesComponent)
  },

  {
    path: 'events',
    loadComponent: () =>
      import('./components/events/events.component')
        .then(m => m.EventsComponent)
  },

  {
  path: 'organizer/events',
  loadComponent: () =>
    import('./components/organizer-create-event/organizer-create-event.component')
      .then(m => m.OrganizerCreateEventComponent),
  // canActivate: [adminGuard]  // ili organizerGuard ako imate
},


  {
    path: '**',
    redirectTo: ''
  }

  


];
