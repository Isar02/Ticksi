import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';




@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  styleUrl: './home.component.scss',
  templateUrl: './home.component.html',
  

})
export class HomeComponent 
{
  constructor(
  private router: Router,
  public authService: AuthService
) {}
organizeEvent(): void {
  if (this.authService.isAuthenticated()) {
    this.router.navigate(['/admin/categories']);
  } else {
    this.router.navigate(['/login']);
  }
}


}
