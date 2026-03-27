import api from './client';
import type { AuthResponse } from './models';

export const authApi = {
  login: async (email: string, password: string): Promise<AuthResponse> => {
    const res = await api.post<AuthResponse>('/auth/login', { email, password });
    return res.data;
  },
  
  register: async (email: string, password: string): Promise<void> => {
    await api.post('/auth/register', { email, password });
  },

  logout: async (): Promise<void> => {
    await api.post('/auth/logout');
  }
};
