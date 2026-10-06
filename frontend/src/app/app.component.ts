import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { FooterComponent } from './components/footer/footer.component';
import { ToastStackComponent } from './components/shared/toast-stack/toast-stack.component';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterModule, NavbarComponent, FooterComponent, ToastStackComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  standalone: true
})
export class AppComponent {
  title = 'Ticksi';
}
