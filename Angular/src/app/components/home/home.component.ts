import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { MANAGER_ROLES } from '../../core/models/role';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  styleUrl: './home.component.scss',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  readonly canOrganize = computed(() =>
    !this.authService.isAuthenticated() || this.authService.hasAnyRole(MANAGER_ROLES));

  constructor(
    private router: Router,
    public authService: AuthService
  ) {}

  organizeEvent(): void {
    this.router.navigate(['/admin/categories']);
  }
}
