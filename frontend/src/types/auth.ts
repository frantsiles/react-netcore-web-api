export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  email: string;
  fullName: string;
  permissions: string[];
}

export interface AuthUser {
  email: string;
  fullName: string;
  permissions: string[];
}
