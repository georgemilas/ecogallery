'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import '../../login/components/login.css';
import { AuthenticatedImage } from '@/app/utils/AuthenticatedImage';

interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  effective_roles: string[];
}

export function CreateRolePage(): JSX.Element {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [parentRoleId, setParentRoleId] = useState<number>(0);
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');

    if (!name.trim()) {
      setError('Role name is required.');
      return;
    }

    setLoading(true);
    try {
      const res = await apiFetch('/api/v1/users/roles', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: name.trim(),
          description: description.trim() || null,
          parent_role_id: parentRoleId || null
        }),
      });
      if (res.ok) {
        setMessage('Role created successfully. Redirecting...');
        setTimeout(() => router.push('/manage-users'), 1500);
      } else {
        const data = await res.json();
        setError(data.message || 'Failed to create role.');
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
        <h1>Create Role</h1>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="roleName">Role Name</label>
            <input
              id="roleName"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter role name"
              required
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="roleDescription">Description</label>
            <input
              id="roleDescription"
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Enter description (optional)"
              disabled={loading}
            />
          </div>
          <div className="form-group">
            <label htmlFor="parentRole">Parent Role</label>
            <select
              id="parentRole"
              value={parentRoleId}
              onChange={(e) => setParentRoleId(Number(e.target.value))}
              disabled={loading}
            >
              <option value={0}>None (standalone role)</option>
              {(() => {
                const excluded = ['public'];
                const isClientRole = (r: RoleInfo) => r.name !== 'client' && r.effective_roles.includes('client') && !r.effective_roles.includes('private');
                const base = roles.filter(r => !excluded.includes(r.name) && !isClientRole(r));
                const client = roles.filter(r => !excluded.includes(r.name) && isClientRole(r));
                return (
                  <>
                    {base.length > 0 && (
                      <optgroup label="Base Roles">
                        {base.map(r => (
                          <option key={r.id} value={r.id}>{r.name}</option>
                        ))}
                      </optgroup>
                    )}
                    {client.length > 0 && (
                      <optgroup label="Client Roles">
                        {client.map(r => (
                          <option key={r.id} value={r.id}>{r.name}</option>
                        ))}
                      </optgroup>
                    )}
                  </>
                );
              })()}
            </select>
            {parentRoleId > 0 && (() => {
              const parent = roles.find(r => r.id === parentRoleId);
              return parent && parent.effective_roles.length > 0 ? (
                <div style={{ marginTop: '6px', display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
                  <span style={{ fontSize: '0.8em', color: '#777' }}>Inherits:</span>
                  {parent.effective_roles.map(er => (
                    <span key={er} style={{
                      display: 'inline-block',
                      padding: '2px 8px',
                      fontSize: '0.8em',
                      borderRadius: '12px',
                      background: '#555',
                      color: '#fff'
                    }}>{er}</span>
                  ))}
                </div>
              ) : null;
            })()}
          </div>
          {error && <div className="error-message">{error}</div>}
          {message && <div className="success-message">{message}</div>}
          <button type="submit" className="login-button" disabled={loading}>
            {loading ? 'Creating...' : 'Create Role'}
          </button>
        </form>
        <div style={{ textAlign: 'center', marginTop: '1em' }}>
          <a href="#" onClick={e => { e.preventDefault(); router.push('/manage-users'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Back to Invite User
          </a><br/>  
          <a href="#" onClick={e => { e.preventDefault(); router.push('/album'); }}
            style={{ color: '#667eea', textDecoration: 'underline', fontSize: '15px' }}>Back to Gallery
          </a>
        </div>
      </div>
    </div>
  );
}
