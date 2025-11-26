import { WeatherReport, CityWeather } from './components/WeatherReport';

async function getReports(): Promise<CityWeather[]> {
  const base = process.env.NEXT_PUBLIC_WEATHER_API_BASE ?? 'http://localhost:5242';
  try {
    const res = await fetch(`${base}/api/v1/weather`, { next: { revalidate: 60 } });
    if (!res.ok) {
      console.error('Failed to fetch weather', res.status);
      return [];
    }
    return await res.json();
  } catch (e) {
    console.error('Error fetching weather', e);
    return [];
  }
}

export default async function Page() {
  const reports = await getReports();
  return (
    <main>
      <WeatherReport reports={reports} />
    </main>
  );
}
