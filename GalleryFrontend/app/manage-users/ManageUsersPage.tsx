'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import '../login/components/login.css';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';

interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  effective_roles: string[];
}

export function ManageUsersPage(): JSX.Element {
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [roleId, setRoleId] = useState<number>(0);
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  useEffect(() => {
    const fetchRoles = async () => {
      try {
        const res = await apiFetch('/api/v1/users/roles');
        if (res.ok) {
          const data = await res.json();
          setRoles(data);
        }
      } catch {
        console.error('Failed to fetch roles');
      }
    };
    fetchRoles();
  }, []);

  const selectedRole = roles.find(r => r.id === roleId);

  const excludedNames = ['public', 'client'];
  const isClientRole = (r: RoleInfo) => r.effective_roles.includes('client') && !r.effective_roles.includes('private');
  const clientRoles = roles.filter(r => !excludedNames.includes(r.name) && isClientRole(r));
  const baseRoles = roles.filter(r => !excludedNames.includes(r.name) && !isClientRole(r));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');

    if (!email || !name) {
      setError('Both email and name are required.');
      return;
    }
    if (!roleId) {
      setError('A role must be selected.');
      return;
    }

    setLoading(true);
    try {
      const res = await apiFetch('/api/v1/users/invite', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, name, role_id: roleId }),
      });
      if (res.ok) {
        setMessage('Invitation sent successfully. The user will receive an email with a registration link.');
        setEmail('');
        setName('');
        setRoleId(0);
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
          <div className="form-group">
            <label htmlFor="role">Role</label>
            <select
              id="role"
              value={roleId}
              onChange={(e) => setRoleId(Number(e.target.value))}
              required
              disabled={loading}
            >
              <option value={0} disabled>Select a role</option>
              {baseRoles.length > 0 && (
                <optgroup label="Base Roles">
                  {baseRoles.map(r => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </optgroup>
              )}
              {clientRoles.length > 0 && (
                <optgroup label="Client Roles">
                  {clientRoles.map(r => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </optgroup>
              )}
            </select>
            {selectedRole && selectedRole.description && (
              <div style={{ marginTop: '4px', fontSize: '0.85em', color: '#777' }}>{selectedRole.description}</div>
            )}
            {selectedRole && selectedRole.effective_roles.length > 0 && (
              <div style={{ marginTop: '6px', display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
                {selectedRole.effective_roles.map(er => (
                  <span key={er} style={{
                    display: 'inline-block',
                    padding: '2px 8px',
                    fontSize: '0.8em',
                    borderRadius: '12px',
                    background: er === selectedRole.name ? '#4a90d9' : '#555',
                    color: '#fff'
                  }}>{er}</span>
                ))}
              </div>
            )}
            <div style={{ marginTop: '6px', textAlign: 'right' }}>
              <a href="/manage-users/create-role" style={{ fontSize: '0.85em', color: '#667eea' }}>+ Create Role</a>
            </div>
          </div>
          {error && <div className="error-message">{error}</div>}
          {message && <div className="success-message">{message}</div>}
          <button type="submit" className="login-button" disabled={loading || !roleId}>
            {loading ? 'Sending...' : 'Send Invitation'}
          </button>
        </form>
        <div style={{ textAlign: 'center', marginTop: '1em' }}>
          <a href="#" onClick={e => { e.preventDefault(); router.push('/album'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Back to Gallery</a>
        </div>
      </div>
    </div>
  );
}
