// Request DTOs
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
}

// Response DTOs
export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  email: string;
  publicId: string;
  firstName: string;
}

// User state for storing in app
export interface UserInfo {
  email: string;
  publicId: string;
  firstName: string;
  role: string | null;
}

