'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import '../login/components/login.css';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';

export function ManageUsersPage(): JSX.Element {
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');

    if (!email || !name) {
      setError('Both email and name are required.');
      return;
    }

    setLoading(true);
    try {
      const res = await apiFetch('/api/v1/users/invite', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, name }),
      });
      if (res.ok) {
        setMessage('Invitation sent successfully. The user will receive an email with a registration link.');
        setEmail('');
        setName('');
      } else {
        const data = await res.json();
        setError(data.message || 'Failed to send invitation.');
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
        <h1>Invite User</h1>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter email address"
              required
              autoComplete="email"
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="name">Name</label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter full name"
              required
              disabled={loading}
            />
          </div>
          {error && <div className="error-message">{error}</div>}
          {message && <div className="success-message">{message}</div>}
          <button type="submit" className="login-button" disabled={loading}>
            {loading ? 'Sending...' : 'Send Invitation'}
          </button>
        </form>
        <button className="login-button" style={{marginTop: '1em'}} onClick={() => router.push('/album')} disabled={loading}>
          Back to Gallery
        </button>
      </div>
    </div>
  );
}
