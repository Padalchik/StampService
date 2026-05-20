import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { unauthorizedEventName } from '../api/apiClient';
import { authStorage } from './authStorage';

type AuthState = {
  token: string | null;
  expiresAt: string | null;
  isAuthenticated: boolean;
  signIn: (token: string, expiresAt: string) => void;
  signOut: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => {
    if (authStorage.isExpired()) {
      authStorage.clear();
      return null;
    }

    return authStorage.getToken();
  });
  const [expiresAt, setExpiresAt] = useState<string | null>(() => authStorage.getExpiresAt());

  const signIn = useCallback((nextToken: string, nextExpiresAt: string) => {
    authStorage.save(nextToken, nextExpiresAt);
    setToken(nextToken);
    setExpiresAt(nextExpiresAt);
  }, []);

  const signOut = useCallback(() => {
    authStorage.clear();
    setToken(null);
    setExpiresAt(null);
  }, []);

  useEffect(() => {
    window.addEventListener(unauthorizedEventName, signOut);
    return () => window.removeEventListener(unauthorizedEventName, signOut);
  }, [signOut]);

  const value = useMemo<AuthState>(
    () => ({
      token,
      expiresAt,
      isAuthenticated: Boolean(token),
      signIn,
      signOut
    }),
    [expiresAt, signIn, signOut, token]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
