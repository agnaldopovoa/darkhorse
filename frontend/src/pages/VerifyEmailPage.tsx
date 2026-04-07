import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { ShieldAlert, ShieldCheck, Loader2 } from 'lucide-react';

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();

  const [status, setStatus] = useState<'loading' | 'success' | 'error' | 'expired'>('loading');
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('Invalid or missing verification token.');
      return;
    }

    const verify = async () => {
      try {
        const res = await authApi.verifyEmail(token);
        if (res.success) {
          setStatus('success');
          setMessage(res.message);
          setTimeout(() => {
            navigate('/login?verified=1');
          }, 3000);
        } else {
          setStatus(res.expired ? 'expired' : 'error');
          setMessage(res.message);
        }
      } catch (err) {
        setStatus('error');
        setMessage('Network error verifying your email. Please try again.');
      }
    };

    verify();
  }, [token, navigate]);

  return (
    <div className="flex h-screen w-full items-center justify-center bg-background px-4">
      <div className="w-full max-w-md bg-surface p-8 shadow-2xl rounded-2xl border border-white/5 flex flex-col items-center text-center">
        
        {status === 'loading' && (
          <>
            <Loader2 className="h-12 w-12 text-primary animate-spin mb-4" />
            <h1 className="text-xl font-bold text-white mb-2">Verifying Email</h1>
            <p className="text-muted text-sm">Please wait while we verify your account securely...</p>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="h-12 w-12 rounded-full bg-success/10 flex items-center justify-center mb-4 text-success">
              <ShieldCheck className="h-6 w-6" />
            </div>
            <h1 className="text-xl font-bold text-white mb-2">Email Verified</h1>
            <p className="text-success text-sm mb-6">{message}</p>
            <p className="text-muted text-xs">Redirecting you to login...</p>
          </>
        )}

        {status === 'expired' && (
          <>
            <div className="h-12 w-12 rounded-full bg-warning/10 flex items-center justify-center mb-4 text-warning">
              <ShieldAlert className="h-6 w-6" />
            </div>
            <h1 className="text-xl font-bold text-white mb-2">Link Expired</h1>
            <p className="text-muted text-sm mb-6">This verification link has expired.</p>
            <Link to="/login" className="btn btn-primary w-full justify-center">
              Return to Login to Resend
            </Link>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="h-12 w-12 rounded-full bg-danger/10 flex items-center justify-center mb-4 text-danger">
              <ShieldAlert className="h-6 w-6" />
            </div>
            <h1 className="text-xl font-bold text-white mb-2">Verification Failed</h1>
            <p className="text-danger text-sm mb-6">{message}</p>
            <Link to="/login" className="btn btn-outline border-border w-full justify-center text-text hover:bg-white/5">
              Return to Login
            </Link>
          </>
        )}

      </div>
    </div>
  );
}
