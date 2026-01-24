import { NextRequest, NextResponse } from 'next/server';

/**
 * Proxy endpoint for serving media files with X-API-Key authentication.
 * This allows native HTML elements (<img>, <video>) to access authenticated media
 * by routing through this API endpoint which adds the required X-API-Key header.
 * 
 * Usage: <video src="/api/media/pictures/2024/video.mp4" />
 */
export async function GET(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) 
{
    const apiKey = process.env.NEXT_PUBLIC_API_KEY || 'dev-secret-key-change-in-production';
  
    // Note: We can't require X-API-Key from client because native <video> tags can't send custom headers
    // Security relies on:  
    // 1. Session cookies for private album access
    // 2. Obscurity - only the app knows to use /api/media/ route
    // This is a tradeoff to support video range requests
    
    // Reconstruct the media path
    const mediaPath = params.path.join('/');
    
    // Use nginx as the proxy - it will handle X-Accel-Redirect properly
    // In Docker: INTERNAL_NGINX_URL should be set to http://nginx:80
    // On host: defaults to http://localhost:80 (or whatever HTTP_PORT is set to)
    const nginxBase = process.env.INTERNAL_NGINX_URL || 'http://localhost:80';
    const mediaUrl = `${nginxBase}/${mediaPath}`;
    
    
    // Forward the request with authentication headers
    const headers: Record<string, string> = {};
    const apiKeyHeader = request.headers.get('x-api-key');
    if (apiKeyHeader) {
        headers['X-API-Key'] = apiKeyHeader;
    }
    else {
        headers['X-API-Key'] = apiKey;
    };
 
  
    // Get session token from cookies (note: cookie name uses underscore)
    const sessionToken = request.cookies.get('session_token')?.value;
    if (sessionToken) {
        headers['Authorization'] = `Bearer ${sessionToken}`;
    }
    const cookieHeader = request.headers.get('cookie');
    if (cookieHeader) {
        headers['Cookie'] = cookieHeader;
    }
    
    // Forward Range header for video seeking
    const rangeHeader = request.headers.get('range');
    if (rangeHeader) {
        headers['Range'] = rangeHeader;
    }
  
    try {
        console.log('Media fetch request:', mediaUrl, headers);    
        const response = await fetch(mediaUrl, {
        headers,
        credentials: 'include',
        });
        
        if (!response.ok && response.status !== 206) {
        console.log('Media fetch error:', response.status, response.statusText, response.headers);    
        return new NextResponse('Media not found or unauthorized', { status: response.status });
        }
        
        // Forward the response with all headers (including Content-Range for 206)
        const responseHeaders = new Headers();
        response.headers.forEach((value, key) => {
        responseHeaders.set(key, value);
        });
        
        // Add CORS headers to allow credentials
        responseHeaders.set('Access-Control-Allow-Credentials', 'true');
        responseHeaders.set('Access-Control-Allow-Origin', request.headers.get('origin') || '*');
        
        return new NextResponse(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers: responseHeaders,
        });
    } catch (error) {
        console.error('Error proxying media:', error);
        return new NextResponse('Error loading media', { status: 500 });
    }
}

export async function OPTIONS(request: NextRequest) {
  return new NextResponse(null, {
    status: 200,
    headers: {
      'Access-Control-Allow-Credentials': 'true',
      'Access-Control-Allow-Origin': request.headers.get('origin') || '*',
      'Access-Control-Allow-Methods': 'GET, OPTIONS',
      'Access-Control-Allow-Headers': 'Range, Authorization',
    },
  });
}
