import React from 'react';

export type PicturesHierarchy = {
  id: number;
  name: string;
  is_album: boolean;
  navigation_path_segments: Array<string>;
  image_path: string;
  last_updated_utc: Date;
};    

export function PicturesHierarchyService({ pictures }: { pictures: PicturesHierarchy[] }) {
  if (!pictures?.length) {
    return <p>No data.</p>;
  }
  return (
    <table style={{ borderCollapse: 'collapse', width: '100%' }}>
      <thead>
        <tr>
          <th style={th}>Id</th>
          <th style={th}>Name</th>
          <th style={th}>Is Album</th>
          <th style={th}>Navigation Path Segments</th>
          <th style={th}>Image Path</th>
          <th style={th}>Last Updated UTC</th>
        </tr>
      </thead>
      <tbody>
        {pictures.map(r => (
          <tr key={r.id}>
            <td style={td}>{r.id}</td>
            <td style={td}>{r.name}</td>
            <td style={td}>{r.is_album ? "Yes" : "No"}</td>
            <td style={td}>{r.navigation_path_segments}</td>   
            <td style={td}>{r.image_path}</td> 
            <td style={td}>{r.last_updated_utc.toString()}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

const th: React.CSSProperties = { border: '1px solid #ccc', padding: '4px', background: '#eee' };
const td: React.CSSProperties = { border: '1px solid #ccc', padding: '4px' };
