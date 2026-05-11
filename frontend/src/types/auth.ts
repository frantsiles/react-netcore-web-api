export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  sessionId: string;
  email: string;
  fullName: string;
  permissions: string[];
}

export interface AuthUser {
  email: string;
  fullName: string;
  permissions: string[];
  sessionId: string;
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
  sessionId: string;
}
