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

  const validateSession = async (): Promise<boolean> => {
    try {
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/auth/validate` : `/api/v1/auth/validate`;
      const token = localStorage.getItem('sessionToken');

      const response = await apiFetch(url, {
        method: 'GET',
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      });

      const data = await response.json();

      if (response.ok && data.success) {
        setUser(data.user);
        localStorage.setItem('user', JSON.stringify(data.user));
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
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/auth/login` : `/api/v1/auth/login`;
      
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
      const apiBase = process.env.NEXT_PUBLIC_API_BASE || '';
      const url = apiBase ? `${apiBase}/api/v1/auth/logout` : `/api/v1/auth/logout`;
      
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

      // If we have no local auth hints (no user and no token), skip hitting the backend and redirect
      if (!storedUser && !token) {
        setUser(null);
        setLoading(false);

        if (pathname !== '/login' && pathname !== '/valbum' && !pathname.startsWith('/valbum?')) {
          router.push('/login');
        }
        return;
      }

      // Validate with server when we have something to validate (cookie and/or token)
      const isValid = await validateSession();
      
      setLoading(false);

      // Redirect to login if not authenticated and not already on login or public pages
      if (!isValid && pathname !== '/login' && pathname !== '/valbum' && !pathname.startsWith('/valbum?')) {
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
