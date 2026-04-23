// ============================================================
//  3TA RESTAURANT — authService.js
//  JWT + User Session management (LocalStorage)
//  All auth-dependent components must import from here
// ============================================================

const STORAGE_KEYS = {
  TOKEN:    'tta_token',
  ROLE:     'tta_role',
  USERNAME: 'tta_username',
};

/**
 * Auth Service — singleton state
 * Import this in every component that needs role/checking
 */
export const Auth = {
  getToken:    () => localStorage.getItem(STORAGE_KEYS.TOKEN),
  getRole:     () => localStorage.getItem(STORAGE_KEYS.ROLE),
  getUsername: () => localStorage.getItem(STORAGE_KEYS.USERNAME),

  isLoggedIn: () => !!localStorage.getItem(STORAGE_KEYS.TOKEN),

  isAdmin: () => localStorage.getItem(STORAGE_KEYS.ROLE) === 'Admin',

  isUser: () => localStorage.getItem(STORAGE_KEYS.ROLE) === 'User',

  /**
   * Save JWT + user info after login
   * @param {{ token: string, role: string, user: { username: string } }} data
   */
  save(data) {
    localStorage.setItem(STORAGE_KEYS.TOKEN,    data.token);
    localStorage.setItem(STORAGE_KEYS.ROLE,     data.role);
    localStorage.setItem(STORAGE_KEYS.USERNAME, data.user?.username ?? '');
  },

  clear() {
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.ROLE);
    localStorage.removeItem(STORAGE_KEYS.USERNAME);
  },

  /**
   * Guard: redirect to login if not authenticated
   * Call at top of every protected page init
   */
  requireAuth(redirectTo = '/login.html') {
    if (!this.isLoggedIn()) {
      window.location.href = redirectTo;
    }
  },

  /**
   * Guard: redirect if not Admin
   */
  requireAdmin(redirectTo = '/index.html') {
    if (!this.isAdmin()) {
      window.location.href = redirectTo;
    }
  },
};

export default Auth;
