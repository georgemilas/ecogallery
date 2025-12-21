'use client';

/**
 * Wrapper around fetch that automatically includes:
 * 1. App API key (X-API-Key header) - required for all API calls
 * 2. User session token (Authorization Bearer header) - for authenticated requests
 */
export async function apiFetch(
  url: string,
  options: RequestInit = {}
): Promise<Response> {
  const headers: Record<string, string> = {
    ...((options.headers as Record<string, string>) || {}),
  };

  // Add app API key (required for all API calls)
  const apiKey = process.env.NEXT_PUBLIC_API_KEY || 'dev-secret-key-change-in-production';
  if (!headers['X-API-Key']) {
    headers['X-API-Key'] = apiKey;
  }

  // Add user session token if we have one
  const token = localStorage.getItem('sessionToken');
  if (token && !headers['Authorization']) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return fetch(url, {
    ...options,
    credentials: 'include',
    headers,
  });
}
