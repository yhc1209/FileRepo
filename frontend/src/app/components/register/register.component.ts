import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { extractErrorMessage } from '../../utils/error-handler.util';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  username = '';
  email = '';
  password = '';
  confirmPassword = '';

  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onRegister(): void {
    if (!this.username || !this.email || !this.password) {
      this.errorMessage.set('請填寫所有欄位');
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage.set('密碼長度至少需 6 個字元');
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('兩次輸入的密碼不一致');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.register({
      username: this.username,
      email: this.email,
      password: this.password
    }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.isSuccess) {
          this.router.navigate(['/files']);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = extractErrorMessage(err, '註冊失敗，請重試');
        this.errorMessage.set(msg);
      }
    });
  }
}
