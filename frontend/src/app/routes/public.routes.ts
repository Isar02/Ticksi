import { Routes } from '@angular/router';
import { EventDetailsComponent } from '../components/event-details/event-details.component';
import { EventsComponent } from '../components/events/events.component';
import { HomeComponent } from '../components/home/home.component';
import { PublicCategoriesComponent } from '../components/public-categories/public-categories.component';

export const PUBLIC_ROUTES: Routes = [
  { path: '', pathMatch: 'full', component: HomeComponent },
  { path: 'events', component: EventsComponent },
  { path: 'event/:id', component: EventDetailsComponent },
  { path: 'categories', component: PublicCategoriesComponent }
];
