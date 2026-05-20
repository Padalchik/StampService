const authTokenKey = 'stampservice.auth.token';
const authExpiresAtKey = 'stampservice.auth.expiresAt';

export const authStorage = {
  getToken(): string | null {
    return localStorage.getItem(authTokenKey);
  },

  getExpiresAt(): string | null {
    return localStorage.getItem(authExpiresAtKey);
  },

  isExpired(): boolean {
    const expiresAt = this.getExpiresAt();
    if (!expiresAt) {
      return false;
    }

    return new Date(expiresAt).getTime() <= Date.now();
  },

  save(token: string, expiresAt: string): void {
    localStorage.setItem(authTokenKey, token);
    localStorage.setItem(authExpiresAtKey, expiresAt);
  },

  clear(): void {
    localStorage.removeItem(authTokenKey);
    localStorage.removeItem(authExpiresAtKey);
  }
};
