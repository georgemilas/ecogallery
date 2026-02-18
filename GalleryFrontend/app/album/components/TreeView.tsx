import React, { useState, useCallback } from 'react';

export interface TreeNode<T> {
  data: T;
  children: TreeNode<T>[];
  expanded: boolean;
}

export interface TreeViewProps<T> {
  /** Flat list of items to build the tree from */
  items: T[];
  /** Extract a unique numeric ID from each item */
  getId: (item: T) => number;
  /** Extract the parent ID from each item (0 or falsy = root) */
  getParentId: (item: T) => number;
  /** Render the label/content for a single node */
  renderLabel: (node: TreeNode<T>) => React.ReactNode;
  /** Optional: render extra actions (buttons) on the right side of each row */
  renderActions?: (node: TreeNode<T>) => React.ReactNode;
  /** Called when a node label is clicked */
  onNodeClick?: (node: TreeNode<T>) => void;
  /** Whether all nodes start expanded (default: true) */
  defaultExpanded?: boolean;
  /** Loading state */
  loading?: boolean;
  /** Error message */
  error?: string | null;
  /** Message shown when tree is empty */
  emptyMessage?: string;
}

function buildTree<T>(
  items: T[],
  getId: (item: T) => number,
  getParentId: (item: T) => number,
  defaultExpanded: boolean,
): TreeNode<T>[] {
  const nodeMap = new Map<number, TreeNode<T>>();
  const roots: TreeNode<T>[] = [];

  for (const item of items) {
    nodeMap.set(getId(item), { data: item, children: [], expanded: defaultExpanded });
  }

  for (const item of items) {
    const id = getId(item);
    const parentId = getParentId(item);
    const node = nodeMap.get(id)!;
    if (!parentId) {
      roots.push(node);
    } else {
      const parent = nodeMap.get(parentId);
      if (parent) {
        parent.children.push(node);
      } else {
        roots.push(node);
      }
    }
  }

  return roots;
}

function toggleNode<T>(nodes: TreeNode<T>[], targetId: number, getId: (item: T) => number): TreeNode<T>[] {
  return nodes.map(n => {
    if (getId(n.data) === targetId) {
      return { ...n, expanded: !n.expanded };
    }
    if (n.children.length > 0) {
      return { ...n, children: toggleNode(n.children, targetId, getId) };
    }
    return n;
  });
}

export function TreeView<T>({
  items,
  getId,
  getParentId,
  renderLabel,
  renderActions,
  onNodeClick,
  defaultExpanded = true,
  loading,
  error,
  emptyMessage = 'No items found',
}: TreeViewProps<T>): JSX.Element {
  const [treeData, setTreeData] = useState<TreeNode<T>[]>([]);
  const [prevItems, setPrevItems] = useState<T[]>([]);

  // Rebuild tree when items change (derived state pattern)
  if (items !== prevItems) {
    setPrevItems(items);
    setTreeData(buildTree(items, getId, getParentId, defaultExpanded));
  }

  const handleToggle = useCallback((id: number) => {
    setTreeData(prev => toggleNode(prev, id, getId));
  }, [getId]);

  const renderNode = (node: TreeNode<T>, level: number = 0) => {
    const hasChildren = node.children.length > 0;
    const id = getId(node.data);

    return (
      <div key={id}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '4px 8px',
            paddingLeft: `${8 + level * 16}px`,
            cursor: 'pointer',
            borderRadius: '4px',
            gap: '4px',
          }}
          onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#3a3a3a')}
          onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
        >
          <button
            onClick={(e) => { e.stopPropagation(); handleToggle(id); }}
            style={{
              background: 'none', border: 'none', cursor: 'pointer',
              padding: '0 2px', color: '#888', fontSize: '12px', width: '16px', flexShrink: 0,
            }}
          >
            {hasChildren ? (node.expanded ? '\u25BC' : '\u25B6') : '\u00B7'}
          </button>
          <span
            onClick={() => onNodeClick?.(node)}
            style={{ color: '#ddd', fontSize: '13px', flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
          >
            {renderLabel(node)}
          </span>
          {renderActions && (
            <div style={{ display: 'flex', gap: '2px', flexShrink: 0 }}>
              {renderActions(node)}
            </div>
          )}
        </div>
        {node.expanded && node.children.map(child => renderNode(child, level + 1))}
      </div>
    );
  };

  return (
    <>
      {loading && <div style={{ color: '#888', fontSize: '13px', padding: '10px' }}>Loading...</div>}
      {error && <div style={{ color: '#dc3545', fontSize: '13px', padding: '10px' }}>{error}</div>}
      <div style={{ flex: 1, overflowY: 'auto' }}>
        {treeData.map(node => renderNode(node))}
        {!loading && treeData.length === 0 && !error && (
          <div style={{ color: '#888', fontSize: '13px', padding: '10px' }}>{emptyMessage}</div>
        )}
      </div>
    </>
  );
}
