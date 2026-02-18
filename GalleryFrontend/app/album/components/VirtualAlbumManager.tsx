import React, { useState, useEffect, useCallback } from 'react';
import { apiFetch } from '@/app/utils/apiFetch';
import { SearchEditorState } from './SearchEditor';
import { DraggablePanel } from './DraggablePanel';
import { TreeView, TreeNode } from './TreeView';

interface AlbumTreeNode {
  id: number;
  album_name: string;
  album_type: string;
  album_description: string;
  feature_image_path: string | null;
  parent_album: string;
  parent_album_id: number;
  role_id: number;
  album_expression: string;
  album_folder: string;
  depth: number;
}

interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  effective_roles: string[];
}

interface AlbumFormData {
  id: number;
  album_name: string;
  album_description: string;
  album_expression: string;
  album_folder: string;
  album_type: string;
  feature_image_path: string;
  parent_album_id: number;
  parent_album_name: string;
  role_id: number;
}

interface VirtualAlbumManagerProps {
  isOpen: boolean;
  onClose: () => void;
  searchEditor?: SearchEditorState;
  onSearchSubmit?: (expression: string, offset: number) => void;
}

const ALBUM_TYPES = [
  { value: 'folder', label: 'Folder' },
  { value: 'expression', label: 'Expression' },
  { value: 'pick_images', label: 'Pick Images' },
];

const getAlbumId = (item: AlbumTreeNode) => item.id;
const getAlbumParentId = (item: AlbumTreeNode) => item.parent_album_id;

export function VirtualAlbumManager({ isOpen, onClose, searchEditor, onSearchSubmit }: VirtualAlbumManagerProps): JSX.Element | null {
  const [view, setView] = useState<'tree' | 'edit'>('tree');
  const [treeData, setTreeData] = useState<AlbumTreeNode[]>([]);
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const [formData, setFormData] = useState<AlbumFormData>({
    id: 0,
    album_name: '',
    album_description: '',
    album_expression: '',
    album_folder: '',
    album_type: 'folder',
    feature_image_path: '',
    parent_album_id: 0,
    parent_album_name: '',
    role_id: 1,
  });

  const fetchTree = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await apiFetch('/api/v1/valbums/tree');
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || 'Failed to load album tree');
      }
      const data: AlbumTreeNode[] = await res.json();
      setTreeData(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchRoles = useCallback(async () => {
    try {
      const res = await apiFetch('/api/v1/users/roles');
      if (res.ok) {
        const data = await res.json();
        setRoles(data);
      }
    } catch (err) {
      // roles will be empty, role select will still work with manual ID
    }
  }, []);

  useEffect(() => {
    if (isOpen) {
      fetchTree();
      fetchRoles();
    }
  }, [isOpen, fetchTree, fetchRoles]);


  const handleEditAlbum = (node: TreeNode<AlbumTreeNode>) => {
    const d = node.data;
    setFormData({
      id: d.id,
      album_name: d.album_name,
      album_description: d.album_description,
      album_expression: d.album_expression,
      album_folder: d.album_folder,
      album_type: d.album_type,
      feature_image_path: d.feature_image_path || '',
      parent_album_id: d.parent_album_id,
      parent_album_name: d.parent_album,
      role_id: d.role_id,
    });
    setConfirmDelete(false);
    setView('edit');
  };

  const handleCreateAlbum = (parentId: number, parentName: string) => {
    setFormData({
      id: 0,
      album_name: '',
      album_description: '',
      album_expression: '',
      album_folder: '',
      album_type: 'folder',
      feature_image_path: '',
      parent_album_id: parentId,
      parent_album_name: parentName,
      role_id: 1,
    });
    setConfirmDelete(false);
    setView('edit');
  };

  const handleSave = async () => {
    if (!formData.album_name.trim()) {
      setError('Album name is required');
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const payload = {
        id: formData.id,
        album_name: formData.album_name.trim(),
        album_description: formData.album_description.trim(),
        album_expression: formData.album_expression.trim(),
        album_folder: formData.album_folder.trim(),
        album_type: formData.album_type,
        feature_image_path: formData.feature_image_path.trim(),
        parent_album_id: formData.parent_album_id,
        role_id: formData.role_id,
        persistent_expression: false,
        is_public: true,
      };
      const res = await apiFetch('/api/v1/valbums/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || 'Failed to save album');
      }
      await fetchTree();
      setView('tree');
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirmDelete) {
      setConfirmDelete(true);
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const res = await apiFetch(`/api/v1/valbums/${formData.id}`, { method: 'DELETE' });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.error || 'Failed to delete album');
      }
      await fetchTree();
      setView('tree');
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
      setConfirmDelete(false);
    }
  };

  const handleOpenSearchEditor = () => {
    if (searchEditor) {
      searchEditor.setText(formData.album_expression);
      searchEditor.setIsOpen(true);
    }
  };

  const handleCaptureExpression = () => {
    if (searchEditor) {
      setFormData(prev => ({ ...prev, album_expression: searchEditor.text }));
    }
  };

  const inputStyle: React.CSSProperties = {
    width: '100%',
    padding: '6px 8px',
    borderRadius: '4px',
    border: '1px solid #555',
    backgroundColor: '#333',
    color: '#ddd',
    fontSize: '13px',
    outline: 'none',
    boxSizing: 'border-box',
  };

  const labelStyle: React.CSSProperties = {
    color: '#aaa',
    fontSize: '12px',
    marginBottom: '3px',
    display: 'block',
  };

  const renderAlbumLabel = useCallback((node: TreeNode<AlbumTreeNode>) => (
    <span title={`${node.data.album_name} (${node.data.album_type})`}>
      {node.data.album_name}
    </span>
  ), []);

  const renderAlbumActions = useCallback((node: TreeNode<AlbumTreeNode>) => (
    <button
      onClick={(e) => { e.stopPropagation(); handleCreateAlbum(node.data.id, node.data.album_name); }}
      title="Add child album"
      style={{
        background: 'none', border: 'none', cursor: 'pointer',
        padding: '0 2px', color: '#e8f09e', fontSize: '14px', fontWeight: 'bold', flexShrink: 0,
      }}
    >
      +
    </button>
  ), []);

  // Title bar content changes based on view
  const title = view === 'tree' ? 'Virtual Albums' : (formData.id === 0 ? 'New Album' : 'Edit Album');

  const titleActions = view === 'tree' ? (
    <>
      <button
        onClick={() => handleCreateAlbum(0, '')}
        title="Add root album"
        style={{
          background: 'none', border: '1px solid #e8f09e', borderRadius: '4px',
          cursor: 'pointer', padding: '2px 8px', color: '#e8f09e', fontSize: '13px',
        }}
      >
        + New
      </button>
      <button
        onClick={onClose}
        style={{ background: 'none', border: 'none', cursor: 'pointer', padding: '2px', display: 'flex', alignItems: 'center' }}
        title="Close (Escape)"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="2" strokeLinecap="round">
          <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
        </svg>
      </button>
    </>
  ) : (
    <>
      <button
        onClick={() => { setView('tree'); setError(null); setConfirmDelete(false); }}
        style={{
          background: 'none', border: '1px solid #888', borderRadius: '4px',
          cursor: 'pointer', padding: '2px 8px', color: '#ddd', fontSize: '12px',
        }}
      >
        Back
      </button>
      <button
        onClick={onClose}
        style={{ background: 'none', border: 'none', cursor: 'pointer', padding: '2px', display: 'flex', alignItems: 'center' }}
        title="Close"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#e8f09e" strokeWidth="2" strokeLinecap="round">
          <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
        </svg>
      </button>
    </>
  );

  const renderTreeView = () => (
    <TreeView<AlbumTreeNode>
      items={treeData}
      getId={getAlbumId}
      getParentId={getAlbumParentId}
      renderLabel={renderAlbumLabel}
      renderActions={renderAlbumActions}
      onNodeClick={handleEditAlbum}
      loading={loading}
      error={error}
      emptyMessage="No virtual albums found"
    />
  );

  const renderEditView = () => {
    const isNew = formData.id === 0;
    const systemRoles = ['public', 'private', 'client', 'admin', 'user_admin', 'album_admin'];
    const excludedNames = ['client', 'user_admin', 'album_admin'];
    const isClientRole = (r: RoleInfo) => !systemRoles.includes(r.name) && r.effective_roles.includes('client');
    const baseRoles = roles.filter(r => !excludedNames.includes(r.name) && !isClientRole(r));
    const clientRoles = roles.filter(r => !excludedNames.includes(r.name) && isClientRole(r));

    return (
      <>
        {error && <div style={{ color: '#dc3545', fontSize: '12px' }}>{error}</div>}

        <div style={{ flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {/* Parent */}
          {formData.parent_album_name && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Parent </label>
              <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0}}>{formData.parent_album_name}</label>
            </div>
          )}

          {/* Name */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Name</label>
            <input
              type="text"
              value={formData.album_name}
              onChange={(e) => setFormData(prev => ({ ...prev, album_name: e.target.value }))}
              style={{ ...inputStyle, flex: 1 }}
              placeholder="Album name"
            />
          </div>

          {/* Description */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Description</label>
            <input
              type="text"
              value={formData.album_description}
              onChange={(e) => setFormData(prev => ({ ...prev, album_description: e.target.value }))}
              style={{ ...inputStyle, flex: 1 }}
              placeholder="Album description"
            />
          </div>

          {/* Feature Image */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Cover Image</label>
            <input
              type="text"
              value={formData.feature_image_path}
              onChange={(e) => setFormData(prev => ({ ...prev, feature_image_path: e.target.value }))}
              style={{ ...inputStyle, fontFamily: 'monospace', flex: 1 }}
              placeholder="\2024\vacation\cover.jpg"
            />
          </div>

          {/* Role */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Role</label>
            <select
              value={formData.role_id}
              onChange={(e) => setFormData(prev => ({ ...prev, role_id: parseInt(e.target.value) }))}
              style={{ ...inputStyle, cursor: 'pointer', flex: 1 }}
            >
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
              {roles.length === 0 && (
                <option value={formData.role_id}>Role ID: {formData.role_id}</option>
              )}
            </select>
          </div>

          {/* Album Type */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ ...labelStyle, marginBottom: 0, flexShrink: 0, minWidth: '70px' }}>Type</label>
            <select
              value={formData.album_type}
              onChange={(e) => setFormData(prev => ({ ...prev, album_type: e.target.value }))}
              style={{ ...inputStyle, cursor: 'pointer', flex: 1 }}
            >
              {ALBUM_TYPES.map(t => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>

          {/* Type-specific fields */}
          {formData.album_type === 'folder' && (
            <div>
              <label style={labelStyle}>Folder Path (relative)</label>
              <input
                type="text"
                value={formData.album_folder}
                onChange={(e) => setFormData(prev => ({ ...prev, album_folder: e.target.value }))}
                style={{ ...inputStyle, fontFamily: 'monospace' }}
                placeholder="\2024\vacation"
              />
            </div>
          )}

          {formData.album_type === 'expression' && (
            <div style={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0 }}>
              <label style={labelStyle}>Search Expression</label>
              <textarea
                value={formData.album_expression}
                onChange={(e) => setFormData(prev => ({ ...prev, album_expression: e.target.value }))}
                style={{
                  ...inputStyle,
                  fontFamily: 'monospace',
                  flex: 1,
                  minHeight: '60px',
                  resize: 'none',
                  overflowY: 'auto',
                }}
                placeholder="e.g. barcelona and 2024"
              />
              <div style={{ display: 'flex', gap: '6px', marginTop: '4px' }}>
                <button
                  onClick={handleOpenSearchEditor}
                  style={{
                    padding: '4px 8px', backgroundColor: '#444', color: '#e8f09e',
                    border: '1px solid #e8f09e', borderRadius: '4px', cursor: 'pointer', fontSize: '12px',
                  }}
                >
                  Open in Search Editor
                </button>
                {searchEditor?.isOpen && (
                  <button
                    onClick={handleCaptureExpression}
                    style={{
                      padding: '4px 8px', backgroundColor: '#444', color: '#4CAF50',
                      border: '1px solid #4CAF50', borderRadius: '4px', cursor: 'pointer', fontSize: '12px',
                    }}
                  >
                    Capture from Editor
                  </button>
                )}
              </div>
            </div>
          )}

          {formData.album_type === 'pick_images' && (
            <div style={{ color: '#888', fontSize: '12px', fontStyle: 'italic', padding: '8px 0' }}>
              TODO - Pick Images functionality coming soon
            </div>
          )}
          
        </div>

        {/* Action buttons */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'space-between', paddingTop: '8px', borderTop: '1px solid #444' }}>
          <div>
            {!isNew && (
              <button
                onClick={handleDelete}
                disabled={saving}
                style={{
                  padding: '6px 12px',
                  backgroundColor: confirmDelete ? '#dc3545' : '#444',
                  color: confirmDelete ? 'white' : '#dc3545',
                  border: '1px solid #dc3545',
                  borderRadius: '4px',
                  cursor: saving ? 'not-allowed' : 'pointer',
                  fontSize: '13px',
                }}
              >
                {confirmDelete ? 'Confirm Delete' : 'Delete'}
              </button>
            )}
          </div>
          <button
            onClick={handleSave}
            disabled={saving}
            style={{
              padding: '6px 16px', backgroundColor: '#e8f09e', color: '#333',
              border: 'none', borderRadius: '4px',
              cursor: saving ? 'not-allowed' : 'pointer', fontWeight: 'bold', fontSize: '13px',
            }}
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </>
    );
  };

  return (
    <DraggablePanel
      isOpen={isOpen}
      title={title}
      titleActions={titleActions}
      defaultPos={{ top: 80, left: 20 }}
      defaultWidth={380}
      defaultHeight={600}
      minHeight={450}
    >
      {view === 'tree' ? renderTreeView() : renderEditView()}
    </DraggablePanel>
  );
}
