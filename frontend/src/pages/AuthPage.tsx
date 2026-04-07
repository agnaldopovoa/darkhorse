import { useState, useEffect } from 'react';
import { useLocation, useNavigate, Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuthStore } from '../store/auth';

export default function AuthPage() {
  const isRegister = useLocation().pathname === '/register';
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const loginAction = useAuthStore((state) => state.login);
  const minLength = Number(import.meta.env.VITE_PASSWORD_MIN_LENGTH) || 12;

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showResend, setShowResend] = useState(false);
  const [resendStatus, setResendStatus] = useState<string | null>(null);
  const [isResending, setIsResending] = useState(false);

  const registered = searchParams.get('registered') === '1';
  const verified = searchParams.get('verified') === '1';

  // Clear states when toggling between login and register
  useEffect(() => {
    setError(null);
    setShowResend(false);
    setResendStatus(null);
  }, [isRegister]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setShowResend(false);
    setResendStatus(null);
    setIsLoading(true);
    try {
      if (isRegister) {
        await authApi.register(email, password);
        navigate('/login?registered=1');
      } else {
        const res = await authApi.login(email, password);
        loginAction(res.accessToken, res.refreshToken);
        navigate('/');
      }
    } catch (err: unknown) {
      const errorResponse = err as { response?: { data?: { detail?: string } }, message?: string };
      const detail = errorResponse.response?.data?.detail || errorResponse.message || 'Authentication failed';
      setError(detail);
      
      if (detail.includes('not verified') || detail.includes('check your inbox')) {
        setShowResend(true);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResend = async () => {
    if (!email) return;
    setIsResending(true);
    setResendStatus(null);
    try {
      await authApi.resendVerification(email);
      setResendStatus('Verification link sent! Check your inbox.');
    } catch (err) {
      setResendStatus('Failed to resend. Please try again.');
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div className="flex h-screen w-full items-center justify-center bg-[#0a0a0b] bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-primary/10 via-background to-background">
      <div className="card w-full max-w-md p-8 shadow-2xl z-10 border border-white/5">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold tracking-tighter text-white">DARKHORSE</h1>
          <p className="text-sm text-muted mt-2">
            {isRegister ? 'Create your platform account' : 'Sign in to access your dashboard'}
          </p>
        </div>

        {registered && !isRegister && !error && (
          <div className="mb-6 p-4 rounded-md bg-success/10 border border-success/20 text-success text-sm text-center font-medium">
            Account created successfully!<br/><span className="text-xs text-muted">Please check your email for the activation link.</span>
          </div>
        )}

        {verified && !isRegister && !error && (
          <div className="mb-6 p-4 rounded-md bg-success/10 border border-success/20 text-success text-sm text-center font-medium">
            Email verified successfully!<br/><span className="text-xs text-muted">You can now sign in.</span>
          </div>
        )}

        {error && (
          <div className="mb-6 p-3 rounded-md bg-danger/10 border border-danger/20 text-danger text-sm text-center flex flex-col gap-2">
            <span>{error}</span>
            {showResend && !isRegister && (
              <button 
                onClick={handleResend}
                disabled={isResending}
                className="text-white underline text-xs font-semibold hover:text-primary transition-colors"
                type="button"
              >
                {isResending ? 'Sending...' : 'Click here to resend link'}
              </button>
            )}
          </div>
        )}

        {resendStatus && (
          <div className="mb-6 p-3 rounded-md bg-primary/10 border border-primary/20 text-primary text-sm text-center font-medium">
            {resendStatus}
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
              minLength={minLength}
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
