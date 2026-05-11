import axios from 'axios';
import type { RefreshResponse } from '../types/auth';

const api = axios.create({
  baseURL: '',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let pendingQueue: Array<{ resolve: (v: string) => void; reject: (e: unknown) => void }> = [];

function drainQueue(token: string | null, error?: unknown) {
  pendingQueue.forEach(({ resolve, reject }) =>
    token ? resolve(token) : reject(error)
  );
  pendingQueue = [];
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    const isExpiredToken =
      error.response?.status === 401 &&
      error.response?.data?.code === 'token_expired' &&
      !originalRequest._retried;

    if (isExpiredToken) {
      originalRequest._retried = true;

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          pendingQueue.push({
            resolve: (token) => {
              originalRequest.headers.Authorization = `Bearer ${token}`;
              resolve(api(originalRequest));
            },
            reject,
          });
        });
      }

      isRefreshing = true;
      const refreshToken = sessionStorage.getItem('refreshToken');

      if (!refreshToken) {
        isRefreshing = false;
        redirectToLogin('session_revoked');
        return Promise.reject(error);
      }

      try {
        const { data } = await axios.post<RefreshResponse>('/bff/auth/refresh', { refreshToken });

        sessionStorage.setItem('token', data.token);
        sessionStorage.setItem('refreshToken', data.refreshToken);

        const storedUser = sessionStorage.getItem('user');
        if (storedUser) {
          const user = JSON.parse(storedUser);
          user.sessionId = data.sessionId;
          sessionStorage.setItem('user', JSON.stringify(user));
        }

        drainQueue(data.token);
        originalRequest.headers.Authorization = `Bearer ${data.token}`;
        return api(originalRequest);
      } catch (refreshError) {
        drainQueue(null, refreshError);
        sessionStorage.clear();
        redirectToLogin('session_revoked');
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    if (error.response?.status === 401 && !originalRequest._retried) {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('refreshToken');
      sessionStorage.removeItem('user');
      window.location.href = '/login';
    }

    return Promise.reject(error);
  }
);

function redirectToLogin(reason?: string) {
  const url = reason ? `/login?reason=${reason}` : '/login';
  window.location.href = url;
}

export default api;
