import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styles: [`
    .navbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 24px;
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      border-radius: 8px;
      margin-bottom: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .brand {
      font-size: 18px;
      font-weight: 700;
      color: #1e293b;
      text-decoration: none;
    }
    .nav-actions {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .user-greeting {
      font-size: 14px;
      color: #475569;
    }
    .btn-logout {
      background: #ef4444;
      color: #ffffff;
      border: none;
      padding: 6px 14px;
      border-radius: 6px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 500;
    }
    .btn-logout:hover {
      background: #dc2626;
    }
    .auth-links a {
      color: #2563eb;
      text-decoration: none;
      font-weight: 500;
      font-size: 14px;
      margin-left: 12px;
    }
  `]
})
export class AppComponent {
  authService = inject(AuthService);
  private router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
