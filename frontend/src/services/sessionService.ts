import api from './api';
import type { AdminSession, Session } from '../types/session';

export const sessionService = {
  getMySessions: async (): Promise<Session[]> => {
    const { data } = await api.get<Session[]>('/bff/sessions/my');
    return data;
  },

  getAllSessions: async (): Promise<AdminSession[]> => {
    const { data } = await api.get<AdminSession[]>('/bff/sessions');
    return data;
  },

  revokeSession: async (sessionId: string): Promise<void> => {
    await api.patch(`/bff/sessions/${sessionId}/revoke`);
  },

  revokeAllMySessions: async (): Promise<void> => {
    await api.delete('/bff/sessions/my');
  },

  revokeAllUserSessions: async (userId: string): Promise<void> => {
    await api.delete(`/bff/admin/users/${userId}/sessions`);
  },
};
