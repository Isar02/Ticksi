import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private apiUrl = 'https://localhost:5001/api/members'; //Da provjeris koji je url moras runnati unutar API ovo dotnet run(U terminalu naravno)
  constructor(private http: HttpClient) { }
  
  getMembers(): Observable<any> {
      return this.http.get(this.apiUrl);
    }
    
  getMember(id: string): Observable<any> {
      return this.http.get(`${this.apiUrl}/${id}`);
  }


}
