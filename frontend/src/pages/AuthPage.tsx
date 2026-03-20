import { useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuthStore } from '../store/auth';

export default function AuthPage() {
  const isRegister = useLocation().pathname === '/register';
  const navigate = useNavigate();
  const loginAction = useAuthStore((state) => state.login);
  
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);
    try {
      if (isRegister) {
        await authApi.register(email, password);
        // After registration, tell user to wait for email
        navigate('/login?registered=1');
      } else {
        const res = await authApi.login(email, password);
        loginAction(res.accessToken, res.refreshToken);
        navigate('/');
      }
    } catch (err: any) {
      setError(err.response?.data?.detail || err.message || 'Authentication failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex h-screen w-full items-center justify-center bg-[#0a0a0b] bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-primary/10 via-background to-background">
      <div className="card w-full max-w-md p-8 shadow-2xl z-10">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold tracking-tighter text-white">DARKHORSE</h1>
          <p className="text-sm text-muted mt-2">
            {isRegister ? 'Create your platform account' : 'Sign in to access your dashboard'}
          </p>
        </div>

        {error && (
          <div className="mb-6 p-3 rounded-md bg-danger/10 border border-danger/20 text-danger text-sm text-center">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-text">Email</label>
            <input 
              type="email" 
              className="input" 
              placeholder="name@example.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required 
            />
          </div>
          
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-text">Password</label>
            <input 
              type="password" 
              className="input" 
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required 
              minLength={12}
            />
          </div>

          <button 
            type="submit" 
            className="btn btn-primary mt-2"
            disabled={isLoading}
          >
            {isLoading ? 'Verifying...' : (isRegister ? 'Create Account' : 'Sign In')}
          </button>
        </form>

        <div className="mt-6 text-center text-sm text-muted">
          {isRegister ? (
            <>Already have an account? <Link to="/login" className="text-primary hover:underline">Sign in</Link></>
          ) : (
            <>Don't have an account? <Link to="/register" className="text-primary hover:underline">Register</Link></>
          )}
        </div>
      </div>
    </div>
  );
}
