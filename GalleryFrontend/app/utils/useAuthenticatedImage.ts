"use client";

import { useState, useEffect } from "react";
import { apiFetch } from "./apiFetch";

/**
 * Hook to load an image with authentication headers and request cancellation
 * Attempts blob conversion first (full X-API-Key security), falls back to API proxy on failure
 *
 * 400px thumbnails are returned directly without blob conversion for performance
 *
 * Strategy:
 *   1. Try blob conversion with X-API-Key (most secure)
 *   2. If blob conversion fails (>10MB, CORS issues, etc), fall back to /api/media/ proxy
 *   3. /api/media/ adds X-API-Key server-side but relies on session cookies + obscurity
 */
export function useAuthenticatedImage(url: string | null | undefined): string | null {
    const [objectUrl, setObjectUrl] = useState<string | null>(null);

    useEffect(() => {
        if (!url) {
            setObjectUrl(null);
            return;
        }

        // Don't fetch blob URLs or data URLs - return them directly
        if (url.startsWith("blob:") || url.startsWith("data:")) {
            setObjectUrl(url);
            return () => {};
        }

        // 400px thumbnails don't need authentication - return URL directly
        if (url.includes("/_thumbnails/400/")) {
            setObjectUrl(url);
            return () => {};
        }

        const abortController = new AbortController();
        let currentObjectUrl: string | null = null;

        // Helper to extract pathname from URL
        const getPath = (urlStr: string) => {
            try {
                const urlObj = new URL(urlStr);
                return urlObj.pathname;
            } catch {
                return urlStr;
            }
        };

        // Try blob conversion directly (no HEAD check to avoid 405 errors)
        console.log("Attempting blob conversion for:", url);
        apiFetch(url, {
            signal: abortController.signal,
            headers: {
                Accept: "image/*",
            },
        })
            .then((res) => {
                console.log("Received response:", res.status, res.statusText, "for", url);

                // Accept both 200 (OK) and 206 (Partial Content)
                if (res.status !== 200 && res.status !== 206) {
                    throw new Error(`Failed to load image: ${res.status}`);
                }

                console.log("Converting to blob...");
                return res.blob();
            })
            .then((blob) => {
                console.log("Blob created:", blob.size, "bytes, type:", blob.type);
                if (abortController.signal.aborted) return;
                currentObjectUrl = URL.createObjectURL(blob);
                console.log("Object URL created:", currentObjectUrl);
                setObjectUrl(currentObjectUrl);
            })
            .catch((err) => {
                if (err.name === "AbortError") {
                    console.log("Request aborted (expected)");
                    return;
                }

                // Blob conversion failed - fall back to API proxy route
                console.warn("Blob conversion failed:", err.message, "- falling back to API proxy");
                const proxyUrl = `/api/media${getPath(url)}`;
                console.log("Using API proxy fallback:", proxyUrl);
                setObjectUrl(proxyUrl);
            });

        // Cleanup: cancel request and revoke object URL when component unmounts or URL changes
        return () => {
            abortController.abort();
            if (currentObjectUrl) {
                URL.revokeObjectURL(currentObjectUrl);
            }
        };
    }, [url]);

    return objectUrl;
}
