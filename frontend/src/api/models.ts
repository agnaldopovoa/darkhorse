export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface UserContext {
  id: string;
  email: string;
}
