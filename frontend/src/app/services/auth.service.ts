import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserDto, VerifyTwoFactorRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = '/api/auth';
  private readonly tokenKey = 'filerepo_token';
  private readonly userKey = 'filerepo_user';

  private currentUserSignal = signal<UserDto | null>(this.getStoredUser());
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.currentUserSignal());

  constructor(private http: HttpClient) {}

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, req).pipe(
      tap(res => {
        if (res.isSuccess && res.accessToken && res.user) {
          this.setSession(res.accessToken, res.user);
        }
      })
    );
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, req).pipe(
      tap(res => {
        if (res.isSuccess && !res.requiresTwoFactor && res.accessToken && res.user) {
          this.setSession(res.accessToken, res.user);
        }
      })
    );
  }

  verifyTwoFactor(req: VerifyTwoFactorRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/verify-2fa`, req).pipe(
      tap(res => {
        if (res.isSuccess && res.accessToken && res.user) {
          this.setSession(res.accessToken, res.user);
        }
      })
    );
  }

  fetchCurrentUser(): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        localStorage.setItem(this.userKey, JSON.stringify(user));
        this.currentUserSignal.set(user);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.currentUserSignal.set(null);
  }

  private setSession(token: string, user: UserDto): void {
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.userKey, JSON.stringify(user));
    this.currentUserSignal.set(user);
  }

  private getStoredUser(): UserDto | null {
    const raw = localStorage.getItem(this.userKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }
}
