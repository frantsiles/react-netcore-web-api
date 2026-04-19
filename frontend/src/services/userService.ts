import api from './api';
import type { User } from '../types/user';

export const userService = {
  getAll: async (): Promise<User[]> => {
    const { data } = await api.get<User[]>('/bff/users');
    return data;
  },
};
