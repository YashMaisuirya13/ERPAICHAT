import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5000/api/auth';
  private tokenKey = 'erp_token';
  private roleKey = 'erp_role';
  private usernameKey = 'erp_username';

  constructor(private http: HttpClient) {}

  login(username: string, role: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/token`, { username, role }).pipe(
      tap(response => {
        if (response && response.success && response.data) {
          localStorage.setItem(this.tokenKey, response.data.token);
          localStorage.setItem(this.roleKey, response.data.role);
          localStorage.setItem(this.usernameKey, response.data.username);
        }
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRole(): string | null {
    return localStorage.getItem(this.roleKey);
  }

  getUsername(): string | null {
    return localStorage.getItem(this.usernameKey);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.usernameKey);
  }
}
