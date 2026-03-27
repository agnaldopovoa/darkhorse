import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.DARKHORSE_API_URL || 'https://localhost:7000/api',
  withCredentials: true, // Required for cookies (CSRF)
});

// Request Interceptor: Add Access Token & CSRF Headers
api.interceptors.request.use(
  (config) => {
    // Attach JWT Access Token
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Attach CSRF Token from cookies if present
    const getCookie = (name: string) => {
      const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
      return match ? match[2] : null;
    };
    const csrfToken = getCookie('csrf_token');
    if (csrfToken) {
      config.headers['X-CSRF-Token'] = csrfToken;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Handle 401 & Automatic Refresh (Scaffold)
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Allow retries for 401s if not already retrying
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) throw new Error('No refresh token');

        // Attempt refresh
        const res = await axios.post(`${api.defaults.baseURL}/auth/refresh`, { refreshToken });

        localStorage.setItem('accessToken', res.data.accessToken);
        localStorage.setItem('refreshToken', res.data.refreshToken);

        // Re-execute original request
        return api(originalRequest);
      } catch (refreshError) {
        // Logout on failure
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
