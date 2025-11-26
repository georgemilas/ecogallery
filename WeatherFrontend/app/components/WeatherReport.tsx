import React from 'react';

export type CityWeather = {
  city: string;
  country: string;
  temperatureCelsius: number;
  temperatureFahrenheit: number;
  humidity: number;
  condition: string;
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
          <th style={th}>Country</th>
          <th style={th}>Temp (°C)</th>
          <th style={th}>Temp (°F)</th>
          <th style={th}>Humidity (%)</th>
          <th style={th}>Condition</th>
        </tr>
      </thead>
      <tbody>
        {reports.map(r => (
          <tr key={r.city}>
            <td style={td}>{r.city}</td>
            <td style={td}>{r.country}</td>
            <td style={td}>{r.temperatureCelsius}</td>
            <td style={td}>{r.temperatureFahrenheit}</td>
            <td style={td}>{r.humidity}</td>
            <td style={td}>{r.condition}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

const th: React.CSSProperties = { border: '1px solid #ccc', padding: '4px', background: '#eee' };
const td: React.CSSProperties = { border: '1px solid #ccc', padding: '4px' };
