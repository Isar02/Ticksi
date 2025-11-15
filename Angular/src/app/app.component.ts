import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from './services/api.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  standalone: true
})
export class AppComponent {
  title = 'Angular';
  members: any[] = [];
  constructor(private apiService : ApiService) {  }
  
ngOnInit(): void {
    this.apiService.getMembers().subscribe({
      next: (data) => this.members = data,
      error: (err) => console.error(err)
    });
  }
 
}
