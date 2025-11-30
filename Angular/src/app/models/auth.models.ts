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
  token: string;
  email: string;
  publicId: string;
}

// Error Response
export interface ApiErrorResponse {
  message: string;
  errors: string[];
}

// User state for storing in app
export interface UserInfo {
  email: string;
  publicId: string;
  token: string;
}

