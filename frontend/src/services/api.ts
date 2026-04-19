import axios from 'axios';

/**
 * Axios instance pointing to the BFF.
 * React never calls the backend API directly — only the BFF.
 * The base URL is read from the Vite env variable VITE_BFF_URL.
 */
const api = axios.create({
  baseURL: import.meta.env.VITE_BFF_URL ?? 'http://localhost:5001',
  headers: { 'Content-Type': 'application/json' },
});

// Attach the bearer token from sessionStorage to every request
api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Redirect to login on 401
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      sessionStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
