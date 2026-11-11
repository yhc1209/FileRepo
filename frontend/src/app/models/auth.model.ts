export interface UserDto {
  id: string;
  username: string;
  email: string;
  isTwoFactorEnabled: boolean;
  twoFactorType: string;
  createdAt: string;
}

export interface AuthResponse {
  isSuccess: boolean;
  message: string;
  requiresTwoFactor: boolean;
  twoFactorToken?: string;
  accessToken?: string;
  user?: UserDto;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface VerifyTwoFactorRequest {
  twoFactorToken: string;
  code: string;
}
