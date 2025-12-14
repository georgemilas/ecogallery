# Weather Frontend (Next.js)

Development:

```bash
cd WeatherFrontend
npm install
npm run dev
```

By default it will attempt to fetch from `http://localhost:5242/api/v1/weather` (adjust the port to the WeatherApi running port).

Configure base URL with `.env.local`:

```
NEXT_PUBLIC_WEATHER_API_BASE=http://localhost:5242
```
