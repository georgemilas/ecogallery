# Authentication Setup Guide

This guide explains how to set up and use the authentication system in the Gallery application.

## Database Setup

1. **Build the database schema:**
   ```bash
   psql -U your_user -d your_database -f GalleryLib/db/db.sql
   ```

## Creating the First Admin User

###  Run create_admin_user.sql

```bash
psql -U your_user -d your_database -f GalleryLib/db/create_admin_user.sql
```

### Registering additional users
Manually update the database to create a user registration token:
1. Create a token in C# or just use any random alphanumerics string (at most 128 characters)
```C#
  var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "");
```

2. add it to the database 
```sql
INSERT INTO user_token (user_id, token, token_type, created_utc, expires_utc, used) 
VALUES (1, '2awpHzEYWEe99YL1ALjr2A', 'user_registration', now() at time zone 'UTC', (now() at time zone 'UTC') + interval '24 hours', false)
```

3. Ask the user to navigate to http://yourGalleryAddress/register?t=2awpHzEYWEe99YL1ALjr2A
4. Alternatively use the app or the API (Swagger, curl or Postman) to do it on their behalf

```bash
curl -X POST http://localhost:5001/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "someuser",
    "email": "someuser@someemail.com",
    "password": "somepassword",
    "fullName": "Full Name",
    "token": "2awpHzEYWEe99YL1ALjr2A"
  }'
```

Then manually update the database to make this user an admin if you wish:

```sql
UPDATE public.user SET is_admin = true WHERE username = 'someuser';
```

## Disable Authentication

In `GalleryApi/Program.cs`, comment the authentication middleware:

```csharp
// app.UseSessionAuth();  
```

* By default all API endpoints require an API key (API requires header 'X-API-Key' to be set to the value configured in appsettings.json)
* By default most API endpoints require an authenticated user (exept /valbum which is public) and this allows access without user authentication 


### Additional Session Management Information
- **Session Duration:** 7 days (configurable in `AuthService.cs`)
- **Session Storage:** PostgreSQL database


