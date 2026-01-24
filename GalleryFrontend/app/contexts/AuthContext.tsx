'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { apiFetch } from '@/app/utils/apiFetch';

interface User {
  id: number;
  username: string;
  email: string;
  full_name?: string;
  is_admin: boolean;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => Promise<void>;
  validateSession: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();
  const pathname = usePathname();

  const setSessionTokenCookie = (token: string) => {
    const isHttps = window.location.protocol === 'https:';
    document.cookie = `session_token=${token}; Path=/; Max-Age=${7 * 24 * 60 * 60}; SameSite=None;${isHttps ? ' Secure' : ''}`;
  }
  
  const validateSession = async (): Promise<boolean> => {
    try {
      const url = `/api/v1/auth/validate`;
      const token = localStorage.getItem('sessionToken');

      const response = await apiFetch(url, {
        method: 'GET',
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });

      const data = await response.json();

      if (response.ok && data.success) {
        setUser(data.user);
        localStorage.setItem('user', JSON.stringify(data.user));
        // Refresh session_token cookie expiration for sliding expiration
        const token = localStorage.getItem('sessionToken');
        if (token) {
          setSessionTokenCookie(token);
        }
        return true;
      } else {
        setUser(null);
        localStorage.removeItem('user');
        localStorage.removeItem('sessionToken');
        return false;
      }
    } catch (err) {
      console.error('Session validation error:', err);
      setUser(null);
      localStorage.removeItem('user');
      return false;
    }
  };

  const login = async (username: string, password: string): Promise<boolean> => {
    try {
      const url = `/api/v1/auth/login`;
      
      const response = await apiFetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        setUser(data.user);
        localStorage.setItem('user', JSON.stringify(data.user));

        const token = data.sessionToken || data.session_token;
        if (token) {
          localStorage.setItem('sessionToken', token);
          setSessionTokenCookie(token);
        }

        return true;
      }
      
      return false;
    } catch (err) {
      console.error('Login error:', err);
      return false;
    }
  };

  const logout = async (): Promise<void> => {
    try {
      const url = `/api/v1/auth/logout`;
      
      await fetch(url, {
        method: 'POST',
        credentials: 'include',
      });
    } catch (err) {
      console.error('Logout error:', err);
    } finally {
      setUser(null);
      localStorage.removeItem('user');
      router.push('/login');
    }
  };

  useEffect(() => {
    // Check for existing session on mount
    const checkAuth = async () => {
      // If we're already on the login page, don't call validate; just mark as unauthenticated
      if (pathname === '/login') {
        setUser(null);
        localStorage.removeItem('user');
        localStorage.removeItem('sessionToken');
        setLoading(false);
        return;
      }

      setLoading(true);
      
      // Try to get user from localStorage first
      const storedUser = localStorage.getItem('user');
      const token = localStorage.getItem('sessionToken');

      if (storedUser) {
        try {
          setUser(JSON.parse(storedUser));
        } catch (e) {
          console.error('Error parsing stored user:', e);
        }
      }


      // List of public routes that do NOT require authentication
      const publicRoutes = [
        '/login',
        '/login/reset-password',
        '/login/set-password',
        '/login/register',
        '/valbum',
        '/',
      ];
      const isPublic = publicRoutes.includes(pathname) || publicRoutes.some(r => pathname.startsWith(r + '?'));

      if (!storedUser && !token) {
        setUser(null);
        setLoading(false);
        if (!isPublic) {
          router.push('/login');
        }
        return;
      }

      // Validate with server when we have something to validate (cookie and/or token)
      const isValid = await validateSession();
      
      setLoading(false);

      // Redirect to login if not authenticated and not already on login or public pages
      if (!isValid && !isPublic) {
        router.push('/login');
      }
    };

    checkAuth();
  }, [pathname]);

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, validateSession }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
