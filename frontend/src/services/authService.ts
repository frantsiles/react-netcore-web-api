import api from './api';
import type { LoginRequest, LoginResponse } from '../types/auth';

export const authService = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/bff/auth/login', credentials);
    return data;
  },

  logout: async (refreshToken: string): Promise<void> => {
    await api.post('/bff/auth/logout', { refreshToken });
  },
};
