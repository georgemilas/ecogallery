# ecogallery Frontend (Next.js)

Development:

```bash
cd GalleryFrontend
npm install
npm run dev
```

By default it will attempt to fetch from `http://localhost:5001/api/v1/album` (adjust the port to the GalleryApi running port).

Configure base URL with `.env.local`:

```
NEXT_PUBLIC_API_BASE=http://localhost:5001
```
