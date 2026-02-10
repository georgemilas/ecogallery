'use client';


import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import './login.css';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';

export function RegisterPage(): JSX.Element {
  const [username, setUsername] = useState('');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token') || '';

  const [roleName, setRoleName] = useState('');
  const [emailReadOnly, setEmailReadOnly] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('Invalid or missing registration token.');
      return;
    }
    const fetchTokenInfo = async () => {
      try {
        const res = await apiFetch(`/api/v1/auth/token-info?token=${encodeURIComponent(token)}`);
        if (res.ok) {
          const data = await res.json();
          if (data.email) {
            setEmail(data.email);
            setEmailReadOnly(true);
          }
          if (data.name) setFullName(data.name);
          if (data.role_name) setRoleName(data.role_name);
        } else {
          setError('Invalid or expired registration token.');
        }
      } catch {
        setError('Unable to validate registration token.');
      }
    };
    fetchTokenInfo();
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');
    if (!username || !fullName || !email || !password || password.length < 6) {
      setError('All fields are required and password must be at least 6 characters.');
      return;
    }
    if (password !== confirm) {
      setError('Passwords do not match.');
      return;
    }
    setLoading(true);
    try {
      const res = await apiFetch('/api/v1/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, fullName, email, password, token }),
      });
      if (res.ok) {
        setMessage('Registration successful. You can now log in.');
        setTimeout(() => router.push('/login'), 3000);
      } else {
        const data = await res.json();
        setError(data.message || 'Registration failed.');
      }
    } catch {
      setError('Unable to connect to server.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <AuthenticatedImage src="/pictures/_thumbnails/1440/public/IMG_8337.jpg" alt="Gallery Logo" style={{width: '90%', marginBottom: '1em'}} />  
      <div className="login-box">
        <h1>Register</h1>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              placeholder="Enter username"
              required
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="fullName">Full Name</label>
            <input
              id="fullName"
              type="text"
              value={fullName}
              onChange={e => setFullName(e.target.value)}
              placeholder="Enter your full name"
              required
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="Enter email"
              required
              disabled={loading}
              readOnly={emailReadOnly}
              style={emailReadOnly ? { backgroundColor: '#e9e9e9', cursor: 'not-allowed' } : undefined}
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Enter password"
              required
              minLength={6}
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="confirm">Confirm Password</label>
            <input
              id="confirm"
              type="password"
              value={confirm}
              onChange={e => setConfirm(e.target.value)}
              placeholder="Confirm password"
              required
              minLength={6}
              disabled={loading}
            />
          </div>
          {roleName && (
            <div style={{ marginBottom: '10px', fontSize: '0.9em', color: '#888' }}>
              Assigned role: <strong>{roleName}</strong>
            </div>
          )}
          {error && <div className="error-message">{error}</div>}
          {message && <div className="success-message">{message}</div>}
          <button type="submit" className="login-button" disabled={loading || !token}>
            {loading ? 'Registering...' : 'Register'}
          </button>
        </form>
      </div>
    </div>
  );
}
