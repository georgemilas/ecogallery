# Authentication Setup Guide

This guide explains how to set up and use the authentication system in the Gallery application.

## Database Setup

1. **Build the database schema:**
   ```bash
   psql -U your_user -d your_database -f GalleryLib/db/db.sql
   ```

## Creating the First Admin User

### Option 1: Run create_admin_user.sql

```bash
psql -U your_user -d your_database -f GalleryLib/db/create_admin_user.sql
```

### Option 2: Using the Register Endpoint

After starting the API, use curl or Postman:

```bash
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "email": "admin@gallery.local",
    "password": "admin123",
    "fullName": "Administrator"
  }'
```

Then manually update the database to make this user an admin:

```sql
UPDATE public.user SET is_admin = true WHERE username = 'admin';
```

## Enabling Authentication

In `GalleryApi/Program.cs`, uncomment the authentication middleware:

```csharp
// app.UseSessionAuth();  // <-- Remove the comment
```

This will require all API endpoints (except `/api/v1/auth/*`) to have a valid session.

## Frontend Usage

### Login Page

Navigate to `/login` to access the login page. Users can:
- Enter username and password
- Get authenticated and redirected to `/album`
- Receive error messages for invalid credentials

### Protected Routes

The `AuthContext` automatically:
- Validates sessions on page load
- Redirects to `/login` if session is invalid
- Stores user info in localStorage for quick access
- Provides `useAuth()` hook for components

### Using the Auth Hook

```typescript
import { useAuth } from '@/app/contexts/AuthContext';

function MyComponent() {
  const { user, loading, logout } = useAuth();

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <p>Welcome, {user?.username}!</p>
      <button onClick={logout}>Logout</button>
    </div>
  );
}
```

## API Endpoints

### POST `/api/v1/auth/login`
Login with username and password.

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@gallery.local",
    "full_name": "Administrator",
    "is_admin": true
  }
}
```

### POST `/api/v1/auth/logout`
Logout and invalidate session.

**Response:**
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

### GET `/api/v1/auth/validate`
Validate current session.

**Response:**
```json
{
  "success": true,
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@gallery.local",
    "full_name": "Administrator",
    "is_admin": true
  }
}
```

### POST `/api/v1/auth/register`
Register a new user.

**Request:**
```json
{
  "username": "newuser",
  "email": "user@example.com",
  "password": "securepassword",
  "fullName": "New User"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

## Session Management

- **Session Duration:** 7 days (configurable in `AuthService.cs`)
- **Session Storage:** PostgreSQL database
- **Session Token:** Secure random token (32 bytes, Base64 encoded)
- **Cookie Settings:**
  - HttpOnly: true (prevents JavaScript access)
  - Secure: true (HTTPS only in production)
  - SameSite: Strict (prevents CSRF attacks)

## Security Considerations

1. **Password Hashing:** Uses SHA-256 (consider upgrading to bcrypt or Argon2 for production)
2. **Session Tokens:** Cryptographically secure random tokens
3. **HTTPS Required:** Cookies marked as Secure (use HTTPS in production)
4. **Session Expiration:** Automatic cleanup of expired sessions
5. **Activity Tracking:** Last activity timestamp updated on each request

## Disabling Authentication (Development)

To disable authentication during development, keep the middleware commented:

```csharp
// app.UseSessionAuth();  // Keep commented to disable auth
```

All endpoints will be accessible without authentication.

## Testing

1. Start the API: `dotnet run --project GalleryApi`
2. Start the frontend: `npm run dev` (in GalleryFrontend)
3. Navigate to `http://localhost:3000/login`
4. Login with your admin credentials
5. You should be redirected to `/album`

## Troubleshooting

### "Authentication required" errors
- Ensure the session cookie is being sent with requests
- Check CORS settings allow credentials
- Verify the session hasn't expired

### Login fails with valid credentials
- Check database connection
- Verify user exists in database
- Check password hash matches

### Session not persisting
- Ensure cookies are enabled in browser
- Check CORS configuration includes `credentials: true`
- Verify cookie domain/path settings
