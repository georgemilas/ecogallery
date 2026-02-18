'use client';


import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import './login.css';


export function ResetPasswordPage(): JSX.Element {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);
    try {
      // Replace with your API endpoint for password reset
      const res = await apiFetch('/api/v1/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      });
      if (res.ok) {
        setMessage('If your email is registered, you will receive a password reset link.');
      } else {
        setError('Failed to send reset email.');
      }
    } catch (err) {
      setError('Unable to connect to server.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <img src="/images/logo.jpg" alt="Gallery Logo" style={{width: '90%', marginBottom: '1em'}} />
      <div className="login-box">
        <h1>Reset Password</h1>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter your email"
              required
              autoComplete="email"
              disabled={loading}
            />
          </div>
          {error && <div className="error-message">{error}</div>}
          {message && <div className="success-message">{message}</div>}
          <button type="submit" className="login-button" disabled={loading}>
            {loading ? 'Sending...' : 'Send Reset Link'}
          </button>
        </form>
        <div style={{ textAlign: 'center', marginTop: '1em' }}>
          <a href="#" className="forgot-password-link" onClick={e => { e.preventDefault(); router.push('/login'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Back to Login
          </a><br/>
          <a href="#" className="public-site-link" onClick={e => { e.preventDefault(); router.push('/valbum'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Go to public site
          </a>
        </div>
      </div>
    </div>
  );
}
