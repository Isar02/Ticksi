import { Routes } from '@angular/router';
import { OrganizerCreateEventComponent } from '../components/organizer-create-event/organizer-create-event.component';
import { OrganizerEventsComponent } from '../components/organizer-events/organizer-events.component';

export const ORGANIZER_ROUTES: Routes = [
  { path: 'events', component: OrganizerEventsComponent },
  { path: 'events/new', component: OrganizerCreateEventComponent },
  { path: 'events/:id/edit', component: OrganizerCreateEventComponent }
];
