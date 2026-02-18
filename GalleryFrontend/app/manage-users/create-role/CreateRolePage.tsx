'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';
import '../../login/components/login.css';


interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  effective_roles: string[];
}

export function CreateRolePage(): JSX.Element {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [parentRoleIds, setParentRoleIds] = useState<number[]>([]);
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  useEffect(() => {
    apiFetch('/api/v1/users/roles')
      .then(res => res.ok ? res.json() : Promise.reject())
      .then(data => setRoles(data))
      .catch(() => console.error('Failed to fetch roles'));
  }, []);

  const handleAddRole = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const id = Number(e.target.value);
    if (id && !parentRoleIds.includes(id)) {
      setParentRoleIds(prev => [...prev, id]);
    }
    e.target.value = '';
  };

  const handleRemoveRole = (id: number) => {
    setParentRoleIds(prev => prev.filter(r => r !== id));
  };

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
          parent_role_ids: parentRoleIds.length > 0 ? parentRoleIds : null
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

  const systemRoles = ['public', 'private', 'client', 'admin', 'user_admin', 'album_admin'];
  const isClientRole = (r: RoleInfo) => !systemRoles.includes(r.name) && r.effective_roles.includes('client');
  const baseRoles = roles.filter(r => r.name !== 'public' && !isClientRole(r));
  const clientRoles = roles.filter(r => isClientRole(r));

  const allInherited = [...new Set(
    parentRoleIds.flatMap(id => {
      const role = roles.find(r => r.id === id);
      return role ? [role.name, ...role.effective_roles] : [];
    })
  )];

  return (
    <div className="login-container">
      <img src="/images/logo.jpg" alt="Gallery Logo" style={{width: '90%', marginBottom: '1em'}} />
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
            <label>Parent Roles</label>
            <select
              value=""
              onChange={handleAddRole}
              disabled={loading}
            >
              <option value="">-- Select a role to add --</option>
              {baseRoles.filter(r => !parentRoleIds.includes(r.id)).length > 0 && (
                <optgroup label="Base Roles">
                  {baseRoles.filter(r => !parentRoleIds.includes(r.id)).map(r => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </optgroup>
              )}
              {clientRoles.filter(r => !parentRoleIds.includes(r.id)).length > 0 && (
                <optgroup label="Client Roles">
                  {clientRoles.filter(r => !parentRoleIds.includes(r.id)).map(r => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </optgroup>
              )}
            </select>
            {parentRoleIds.length > 0 && (
              <div style={{ marginTop: '8px', display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                {parentRoleIds.map(id => {
                  const role = roles.find(r => r.id === id);
                  return role ? (
                    <span key={id} style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '4px',
                      padding: '4px 10px',
                      fontSize: '0.85em',
                      borderRadius: '14px',
                      background: '#667eea',
                      color: '#fff',
                    }}>
                      {role.name}
                      <button
                        type="button"
                        onClick={() => handleRemoveRole(id)}
                        disabled={loading}
                        style={{
                          background: 'none',
                          border: 'none',
                          color: '#fff',
                          cursor: 'pointer',
                          padding: '0 2px',
                          fontSize: '1.1em',
                          lineHeight: 1,
                          opacity: 0.8,
                        }}
                      >&times;</button>
                    </span>
                  ) : null;
                })}
              </div>
            )}
            {allInherited.length > 0 && (
              <div style={{ marginTop: '6px', display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
                <span style={{ fontSize: '0.8em', color: '#888' }}>Effective Roles:</span>
                {allInherited.map(er => (
                  <span key={er} style={{
                    display: 'inline-block',
                    padding: '2px 8px',
                    fontSize: '0.8em',
                    borderRadius: '12px',
                    background: '#444',
                    color: '#ccc'
                  }}>{er}</span>
                ))}
              </div>
            )}
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
