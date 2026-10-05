import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { MANAGER_ROLES, Role } from '../../core/models/role';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.authService.currentUser;
  readonly canManage = computed(() => this.authService.hasAnyRole(MANAGER_ROLES));
  readonly isAdmin = computed(() => this.authService.hasAnyRole([Role.Admin]));

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
