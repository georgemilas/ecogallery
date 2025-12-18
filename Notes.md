## Configuration and Secrets Management

### Database Password Security
Passwords and sensitive configuration are kept out of Git using:

**Development (User Secrets):**
```powershell
# Set database password for GalleryApi
cd GalleryApi
dotnet user-secrets set "Database:Password" "your-password-here"

# Set database password for GalleryService
cd ..\GalleryService
dotnet user-secrets set "Database:Password" "your-password-here"
```

User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\` and automatically loaded in Development environment.

**Production (Environment Variables):**
```powershell
# Set environment variable (Windows)
$env:Database__Password = "your-production-password"

# Or set permanently in system environment variables
[System.Environment]::SetEnvironmentVariable('Database__Password', 'your-password', 'Machine')
```

**Alternative (appsettings.Local.json):**
Create `appsettings.Local.json` in GalleryApi or GalleryService (already in .gitignore):
```json
{
  "Database": {
    "Password": "your-password"
  }
}
```