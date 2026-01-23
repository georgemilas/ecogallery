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
) {
  const apiKey = process.env.NEXT_PUBLIC_API_KEY || 'dev-secret-key-change-in-production';
  const apiBase = process.env.NEXT_PUBLIC_API_BASE || 'http://localhost';
  
  // Note: We can't require X-API-Key from client because native <video> tags can't send custom headers
  // Security relies on:  
  // 1. Session cookies for private album access
  // 2. Obscurity - only the app knows to use /api/media/ route
  // This is a tradeoff to support video range requests
  
  // Reconstruct the media path
  const mediaPath = params.path.join('/');
  const mediaUrl = `${apiBase}/${mediaPath}`;
  
  // Get session token from cookies (note: cookie name uses underscore)
  const sessionToken = request.cookies.get('session_token')?.value;
  
  // Forward the request with authentication headers to backend
  const headers: Record<string, string> = {
    'X-API-Key': apiKey,  // Add server-side for backend
  };
  
  if (sessionToken) {
    headers['Authorization'] = `Bearer ${sessionToken}`;
  }
  
  // Forward Range header for video seeking
  const rangeHeader = request.headers.get('range');
  if (rangeHeader) {
    headers['Range'] = rangeHeader;
  }
  
  try {
    const response = await fetch(mediaUrl, {
      headers,
      credentials: 'include',
    });
    
    if (!response.ok && response.status !== 206) {
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
