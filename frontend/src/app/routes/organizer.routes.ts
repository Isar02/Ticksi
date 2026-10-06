import { Routes } from '@angular/router';
import { EventWizardComponent } from '../components/event-wizard/event-wizard.component';
import { OrganizerEventsComponent } from '../components/organizer-events/organizer-events.component';

export const ORGANIZER_ROUTES: Routes = [
  { path: 'events', component: OrganizerEventsComponent },
  { path: 'events/new', component: EventWizardComponent },
  { path: 'events/:id/edit', component: EventWizardComponent }
];
