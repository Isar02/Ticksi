import {Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category } from '../models/category.model';

@Injectable({providedIn: 'root'})
export class CategoryService
{
    private apiUrl = 'https://localhost:5001/api/eventcategories'; 

    constructor(private http: HttpClient) {}

    getAll(): Observable<Category[]> 
    {
        return this.http.get<Category[]>(this.apiUrl);
    }

    create(category: Category): Observable<Category> 
    {
        return this.http.post<Category>(this.apiUrl, category);
    }

    update(publicId: string, category: Category): Observable<Category>
    {
        const url = `${this.apiUrl}/${publicId}`;
        return this.http.put<Category>(url, category);
    }

    delete(publicId: string): Observable<void> 
    {
        const url = `${this.apiUrl}/${publicId}`;
        return this.http.delete<void>(url);
    }

    

}