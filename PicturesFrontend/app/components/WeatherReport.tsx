import React from 'react';

export type CityWeather = {
  title: string;
  min_temp: number;
  max_temp: number;
  applicable_date: string  
};

export function WeatherReport({ reports }: { reports: CityWeather[] }) {
  if (!reports?.length) {
    return <p>No data.</p>;
  }
  return (
    <table style={{ borderCollapse: 'collapse', width: '100%' }}>
      <thead>
        <tr>
          <th style={th}>City</th>
          <th style={th}>MinTemp (°C)</th>
          <th style={th}>MaxTemp (°F)</th>
          <th style={th}>Date</th>
        </tr>
      </thead>
      <tbody>
        {reports.map(r => (
          <tr key={r.title}>
            <td style={td}>{r.title}</td>
            <td style={td}>{r.min_temp}</td>
            <td style={td}>{r.max_temp}</td>
            <td style={td}>{r.applicable_date}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

const th: React.CSSProperties = { border: '1px solid #ccc', padding: '4px', background: '#eee' };
const td: React.CSSProperties = { border: '1px solid #ccc', padding: '4px' };
