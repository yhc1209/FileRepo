import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  usernameOrEmail = '';
  password = '';
  twoFactorCode = '';

  isMfaStep = signal(false);
  twoFactorToken = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onLogin(): void {
    if (!this.usernameOrEmail || !this.password) {
      this.errorMessage.set('請輸入帳號與密碼');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login({
      usernameOrEmail: this.usernameOrEmail,
      password: this.password
    }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.requiresTwoFactor && res.twoFactorToken) {
          // Switch to MFA verification step
          this.isMfaStep.set(true);
          this.twoFactorToken.set(res.twoFactorToken);
        } else if (res.isSuccess) {
          this.router.navigate(['/files']);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || '登入失敗，請確認帳號密碼';
        this.errorMessage.set(msg);
      }
    });
  }

  onVerifyMfa(): void {
    const token = this.twoFactorToken();
    if (!token || !this.twoFactorCode) {
      this.errorMessage.set('請輸入 6 位數多因子驗證碼');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.verifyTwoFactor({
      twoFactorToken: token,
      code: this.twoFactorCode
    }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.isSuccess) {
          this.router.navigate(['/files']);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || '多因子驗證失敗，請重新確認代碼';
        this.errorMessage.set(msg);
      }
    });
  }

  cancelMfa(): void {
    this.isMfaStep.set(false);
    this.twoFactorToken.set(null);
    this.twoFactorCode = '';
    this.errorMessage.set(null);
  }
}
