import { Component, HostListener, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { MANAGER_ROLES } from '../../core/models/role';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss'
})
export class NavbarComponent {
  isScrolled = false;
  readonly canManage = computed(() => this.authService.hasAnyRole(MANAGER_ROLES));

  constructor(public authService: AuthService, private router: Router) {}

  @HostListener('window:scroll', [])
  onWindowScroll() {
    this.isScrolled = window.scrollY > 10;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}

