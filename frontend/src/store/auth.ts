import { create } from 'zustand';
import type { UserContext } from '../api/models';
import { jwtDecode } from 'jwt-decode';

interface AuthState {
  user: UserContext | null;
  isAuthenticated: boolean;
  login: (token: string, refreshToken: string) => void;
  logout: () => void;
}

interface JwtPayload {
  sub: string;
  email: string;
  exp: number;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: !!localStorage.getItem('accessToken'),
  login: (token, refresh) => {
    localStorage.setItem('accessToken', token);
    localStorage.setItem('refreshToken', refresh);
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      set({
        isAuthenticated: true,
        user: { id: decoded.sub, email: decoded.email }
      });
    } catch {
      set({ isAuthenticated: false, user: null });
    }
  },
  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    set({ isAuthenticated: false, user: null });
  }
}));

// Initial sync on app load
const initialToken = localStorage.getItem('accessToken');
if (initialToken) {
  try {
    const decoded = jwtDecode<JwtPayload>(initialToken);
    useAuthStore.setState({ 
      user: { id: decoded.sub, email: decoded.email },
      isAuthenticated: true 
    });
  } catch {
    localStorage.removeItem('accessToken');
  }
}
