import { Routes } from '@angular/router';
import { OrganizerCreateEventComponent } from '../components/organizer-create-event/organizer-create-event.component';

export const ORGANIZER_ROUTES: Routes = [
  { path: 'events', component: OrganizerCreateEventComponent }
];
