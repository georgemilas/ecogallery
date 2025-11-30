'use client';

import { WeatherReport, CityWeather } from './components/WeatherReport';
import { useEffect, useState } from 'react';

export default function Page() {
  const [reports, setReports] = useState<CityWeather[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchReports() {
      const base = process.env.NEXT_PUBLIC_WEATHER_API_BASE ?? 'http://localhost:5001';
      try {
        const res = await fetch(`${base}/api/v1/weather`);
        if (!res.ok) {
          console.error('Failed to fetch weather', res.status);
          setReports([]);
        } else {
          const data = await res.json();
          setReports(data);
        }
      } catch (e) {
        console.error('Error fetching weather', e);
        setReports([]);
      } finally {
        setLoading(false);
      }
    }
    fetchReports();
  }, []);

  return (
    <main>
      {loading ? <p>Loading...</p> : <WeatherReport reports={reports} />}
    </main>
  );
}
