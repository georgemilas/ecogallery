# EcoGallery

**Privacy-focused, self-hosted photo & video gallery for managing massive media collections locally.**

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-14-000000)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-latest-336791)](https://www.postgresql.org/)

---

## Overview

EcoGallery is a self-hosted gallery application designed for photographers and families with large photo/video collections who want full control over their media without relying on cloud services.

**Key Features:**
- 🔒 **Privacy-First** - No cloud dependency, no third-party AI training on your data
- 📁 **Folder-Based Albums** - Automatic album organization from your directory structure
- 🔍 **Powerful Search** - Boolean expression queries with custom syntax
- 🎨 **Virtual Albums** - Dynamic collections based on search expressions
- ⚡ **Handles Scale** - Proven with 45K+ images (400GB+/80K files including RAW)
- 🖼️ **8K Display Support** - Full-resolution image viewing
- 🎬 **Video Support** - Metadata extraction and streaming
- 🔄 **Auto-Sync** - Bidirectional file system ↔ database synchronization

> 📖 **[Full Architecture Documentation](architecture.md)** - Deep dive into design patterns, implementation details, and technical decisions.

---

## Screenshots

<table>
<tr>
<td width="50%">

### Gallery View
![Gallery](data/gallery.png)
Browse albums and sub-albums with thumbnail previews. Sort, search, and navigate with keyboard shortcuts.

</td>
<td width="50%">

### Image Viewer
![Viewer](data/gallery-image.png)
Full-screen slideshow, zoom, EXIF metadata display, and keyboard navigation.

</td>
</tr>
</table>

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/)

```powershell
# Install .NET 10 (Windows)
winget install Microsoft.DotNet.SDK.10
```

### 1. Setup Database

```powershell
# Create database
dotnet run --project GalleryService -- create-db -d ecogallery

# Create admin user
psql -U postgres -d ecogallery -f GalleryLib/db/create_admin_user.sql
```

### 2. Initial Sync

```powershell
# Sync file system to database + generate thumbnails
dotnet run --project GalleryService -- sync -f "E:\path\to\pictures"
```

### 3. Start Services

```powershell
# Terminal 1: Start API
dotnet run --project GalleryApi

# Terminal 2: Start Frontend
npm run dev --prefix GalleryFrontend
```

Open **http://localhost** in your browser.

> 🔧 **Configuration:** See [Configuration Guide](architecture.md#configuration-and-setup) for `appsettings.json` and `.env.local` setup.

---

## Architecture

EcoGallery follows a **multi-tier architecture** with clean separation of concerns:

```
┌─────────────────┐
│  GalleryFrontend│  ← Next.js/React (Port 3000)
└────────┬────────┘
         │
┌────────▼────────┐
│   GalleryApi    │  ← .NET 10 Web API (Port 5001)
└────────┬────────┘
         │
    ┌────┴─────┬──────────────┬─────────────┐
    ▼          ▼              ▼             ▼
┌────────┐ ┌────────┐  ┌──────────┐  ┌──────────┐
│GalleryLib│PostgreSQL│ │File System│ │  NGINX   │
└──────────┘└──────────┘ └──────────┘  └──────────┘
     ▲
     │
┌────┴──────┐
│GalleryService│ ← CLI for sync & setup
└────────────┘
```

**Core Projects:**
- **[GalleryLib](architecture.md#a-gallerylib-core-business-logic-library)** - Business logic, repositories, services
- **[GalleryApi](architecture.md#b-galleryapi-rest-api)** - REST API with authentication
- **[GalleryService](architecture.md#c-galleryservice-cli-tool)** - CLI for database sync and thumbnails
- **[GalleryFrontend](architecture.md#d-galleryfrontend-nextjs-react-app)** - React application
- **[ExpParser](architecture.md#e-expparser-git-submodule)** - Custom boolean expression parser (Git submodule)

> 📖 **[Detailed Architecture](architecture.md#overall-architecture)** - Full system interaction diagrams and data flows.

---

## Key Features Explained

### Folder-Based Albums

Each directory becomes an album automatically. Nested folders create album hierarchies.

```
Pictures/
├── 2024/
│   ├── Vacation/
│   │   └── Beach/       ← Album: "2024 > Vacation > Beach"
│   └── Family/
└── 2023/
```

**Feature Images:** Mark a photo as the album cover by naming it `feature_photo.jpg` or `_feature.jpg`.

> 📖 **[How It Works](architecture.md#f-file-system-sync-architecture)** - File system sync architecture details.

### Virtual Albums

Create dynamic collections using boolean search expressions defined in YAML:

```yaml
- name: "Barcelona Trip"
  expression: "barcelona and (2024 8024) and not (raw dcim)"
```

**Search Syntax:**
- Boolean: `AND`, `OR`, `NOT`
- Grouping: `(expression)`
- Regex: `{pattern}`

> 📖 **[Virtual Albums Guide](architecture.md#d-virtual-albums-via-expression-parser)** - Full syntax and examples.

### Auto-Sync

**FileObserverService** monitors your pictures folder and automatically:
- Creates thumbnails (400px, 1440px)
- Extracts EXIF/video metadata
- Updates database records
- Handles file renames, moves, and deletions

Runs every 2 minutes with real-time `FileSystemWatcher` for immediate updates.

> 📖 **[Processor Pattern](architecture.md#the-processor-pattern---deep-dive)** - Deep dive into the composable file processing architecture.

### Recursive Album Queries

Efficient breadcrumb navigation using **PostgreSQL Recursive CTEs**:

```sql
WITH RECURSIVE ancestors AS (
    SELECT id, album_name, parent_album, 0 AS depth
    FROM album WHERE id = @id
    UNION ALL
    SELECT t.id, t.album_name, t.parent_album, a.depth + 1
    FROM album t JOIN ancestors a ON t.album_name = a.parent_album
)
SELECT * FROM ancestors ORDER BY depth;
```

**Result:** Single query returns full hierarchy path (e.g., `Home > Vacations > 2024 > Europe`).

> 📖 **[Recursive CTEs Explained](architecture.md#a-hierarchical-data-with-recursive-ctes)** - Performance analysis and alternatives comparison.

---

## CLI Commands

### Database Sync

```powershell
# Sync file system to database
 $env:DOTNET_ENVIRONMENT="Development"
dotnet run --project GalleryService -- db -f "E:\pictures"
```

### Thumbnail Generation

```powershell
# Generate 400px and 1440px thumbnails
dotnet run --project GalleryService -- thumbnails -h 400 1440 -f "E:\pictures"
```

### Combined Sync

```powershell
# Database + thumbnails in one command
dotnet run --project GalleryService -- sync -f "E:\pictures"
```

### Virtual Albums

```powershell
# Load virtual albums from YAML
dotnet run --project GalleryService -- valbum -f "E:\pictures" -y "virtual albums.yml"
```

### Database Cleanup

```powershell
# Remove orphaned records and thumbnails
dotnet run --project GalleryService -- cleanup -f "E:\pictures"
```

> 📖 **[All Commands](architecture.md#c-galleryservice-cli-tool)** - Complete CLI reference.

---

## Production Deployment

### With NGINX (Recommended)

1. **Build Frontend:**
```powershell
npm run build --prefix GalleryFrontend
npm run start --prefix GalleryFrontend -- --hostname 0.0.0.0 --port 3000
```

2. **Start API:**
```powershell
dotnet run --project GalleryApi --urls "http://0.0.0.0:5001"
```

3. **Configure NGINX:** Use the provided `nginx.config` for reverse proxy, caching, and authentication.

4. **Port Forwarding:** Forward port 80 from your public IP to NGINX.

> 📖 **[Production Setup](architecture.md#production-deployment)** - NGINX configuration, Docker, and AWS deployment.

### Security

**Two-Tier Authentication:**
1. **App Auth** - API key (X-API-Key header) validates frontend ↔ backend
2. **User Auth** - Session-based with 7-day cookies

**File Access:**
- Small thumbnails (400px): Direct NGINX serving
- Large thumbnails: NGINX with API key validation
- Original files: Proxied through API with full authentication

> 📖 **[Security Details](architecture.md#security-setup)** - Authentication flow and secrets management.

---

## Configuration

### Backend (`appsettings.json`)

```json
{
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "ecogallery",
    "Username": "postgres"
  },
  "PicturesData": {
    "Folder": "E:\\pictures",
    "skipSuffix": ["_skip", "-pss", "_noW"],
    "skipPrefix": ["skip_", "draft_"],
    "imageExtensions": [".jpg", ".jpeg", ".png", ".webp"],
    "movieExtensions": [".mp4", ".mov", ".avi", ".3gp"]
  },
  "AppAuth": {
    "ApiKey": "your-api-key-here"
  }
}
```

### Frontend (`.env.local`)

```env
NEXT_PUBLIC_API_URL=http://localhost:5001
NEXT_PUBLIC_API_KEY=your-api-key-here
```

> 📖 **[Full Configuration Guide](architecture.md#configuration-files)** - All settings explained.

---

## Design Patterns

EcoGallery demonstrates production-ready software architecture with **6 classic design patterns**:

1. **[Strategy Pattern](architecture.md#1-strategy-pattern)** - Interchangeable file processors
2. **[Template Method](architecture.md#2-template-method-pattern)** - Base processor with customizable steps
3. **[Composite Pattern](architecture.md#3-composite-pattern)** - CombinedProcessor orchestrates multiple processors
4. **[Observer Pattern](architecture.md#4-observer-pattern)** - FileSystemWatcher monitors file changes
5. **[Factory Pattern](architecture.md#5-factory-pattern)** - CreateProcessor encapsulates construction
6. **[Repository Pattern](architecture.md#6-repository-pattern)** - Data access abstraction

### Composable Processor Pattern

The heart of EcoGallery's file processing:

```csharp
var processors = new List<IFileProcessor> {
    new DbSyncProcessor(config, dbConfig),           // Updates database
    new MultipleThumbnailsProcessor(config, [400, 1440])  // Generates thumbnails
};
var combined = new CombinedProcessor(processors, config);
var service = new FileObserverService(combined, intervalMinutes: 2);
```

**When a new photo is added:**
1. FileSystemWatcher detects file
2. CombinedProcessor executes each processor in sequence
3. Database updated + thumbnails generated in ~150ms
4. Zero manual coordination

> 📖 **[Processor Pattern Deep Dive](architecture.md#j-composable-file-processor-pattern)** - Complete implementation with examples.

---

## Technology Stack

**Backend:**
- .NET 10, ASP.NET Core Web API
- PostgreSQL with pg_trgm for fast search
- MetadataExtractor (EXIF), FFMpegCore (video)
- ImageSharp (image processing)

**Frontend:**
- Next.js 14, React 18, TypeScript 5.4
- Client-side rendering for gallery viewer

**Infrastructure:**
- NGINX (reverse proxy, caching, auth)
- Docker & Terraform (deployment)

> 📖 **[Full Tech Stack](architecture.md#main-technologies-and-frameworks)** - All libraries and frameworks.

---

## Performance

**Optimizations:**
- **Thumbnail Caching:** 7-day NGINX cache reduces server load
- **Recursive CTEs:** 60% latency reduction vs N+1 queries
- **Smart Thumbnails:** Load image once, resize to all needed sizes
- **Parallel Processing:** Configurable degree of parallelism
- **Debouncing:** 300ms delay batches rapid file changes

**Scale Tested:**
- 45,000+ images and videos
- 400GB+ total size (80K files with RAW)
- Sub-5ms breadcrumb queries
- ~150ms full file processing (DB + thumbnails)

> 📖 **[Performance Analysis](architecture.md#key-benefits-of-this-design)** - Detailed benchmarks.

---

## Limitations & Future Work

**Current Limitations:**
- Requires self-hosted server (local or cloud)
- No RAW file support yet
- Internet access requires port forwarding or cloud hosting

**Roadmap:**
- [ ] User management and role-based access control
- [ ] Local AI integration for enhanced search
- [ ] Face detection and recognition
- [ ] RAW file format support
- [ ] Mobile app

---

## File Structure

```
ecogallery/
├── GalleryLib/              # Core business logic
│   ├── model/               # Data models
│   ├── repository/          # Database access
│   ├── service/             # Processors, services
│   └── db/                  # SQL schema
├── GalleryApi/              # REST API
│   ├── Controllers/         # API endpoints
│   ├── Middleware/          # Authentication
│   └── service/             # API services
├── GalleryService/          # CLI tool
│   └── Program.cs           # Command definitions
├── GalleryFrontend/         # Next.js app
│   └── app/                 # Routes & components
├── ExpParser/               # Search parser (submodule)
├── terraform/               # AWS deployment
├── nginx.config             # Reverse proxy config
├── virtual albums.yml       # Virtual album definitions
└── architecture.md          # Complete technical docs
```

> 📖 **[Project Structure](architecture.md#key-file-locations)** - Detailed file organization.

---

## Contributing

This is a personal project, but contributions are welcome! Please:

1. Read the [Architecture Documentation](architecture.md)
2. Check existing issues/features
3. Open an issue to discuss major changes
4. Submit PRs with clear descriptions

---

## Contact & Support

**Need Help?** Contact me for:
- Deployment assistance
- Custom feature requests
- Production setup guidance

---

## License

This project is for personal use. Contact for commercial licensing.

---

## Further Reading

**Technical Deep Dives:**
- 🔄 **[The Processor Pattern](architecture.md#the-processor-pattern---deep-dive)** - Composable file processing architecture
- 🌳 **[Recursive CTEs](architecture.md#a-hierarchical-data-with-recursive-ctes)** - Efficient hierarchical queries
- 🎯 **[Complete Event Flow](architecture.md#complete-event-flow-example)** - File processing lifecycle
- 🔐 **[Security Architecture](architecture.md#security-setup)** - Two-tier authentication

**Getting Started Guides:**
- ⚙️ **[Configuration](architecture.md#configuration-and-setup)** - Complete setup guide
- 🚀 **[Development Workflow](architecture.md#development-workflow)** - Local development
- 📦 **[Production Deployment](architecture.md#production-deployment)** - NGINX, Docker, AWS

**Component Reference:**
- 📚 **[GalleryLib](architecture.md#a-gallerylib-core-business-logic-library)** - Core business logic
- 🌐 **[GalleryApi](architecture.md#b-galleryapi-rest-api)** - REST API endpoints
- 💻 **[GalleryFrontend](architecture.md#d-galleryfrontend-nextjs-react-app)** - React application
- 🔍 **[ExpParser](architecture.md#e-expparser-git-submodule)** - Search expression parser

---

**Built with ❤️ for privacy and performance.**
