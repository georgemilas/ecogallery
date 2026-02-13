#!/bin/sh
# Replace API key placeholder in the built JS bundle with the runtime environment variable.
# This allows pre-built Docker images to work with different API keys per deployment.

if [ -n "$NEXT_PUBLIC_API_KEY" ] && [ "$NEXT_PUBLIC_API_KEY" != "__API_KEY_PLACEHOLDER__" ]; then
    echo "Injecting runtime API key into frontend bundle..."
    find /app/.next -name '*.js' -exec sed -i "s|__API_KEY_PLACEHOLDER__|${NEXT_PUBLIC_API_KEY}|g" {} +
    echo "API key injected."
else
    echo "WARNING: NEXT_PUBLIC_API_KEY is not set or still using placeholder!"
fi

# Execute the CMD (e.g., "node server.js")
exec "$@"