// ============================================================
//  3TA RESTAURANT — apiClient.js
//  Centralized fetch wrapper | API modules only
//  Auth logic lives in js/services/authService.js
// ============================================================

import { Auth } from './services/authService.js';

const BASE_URL = 'http://localhost:5188';

// ── Core Request Builder ─────────────────────────────────────
async function request(endpoint, {
  method   = 'GET',
  body     = null,
  isForm   = false,
  auth     = true,
  headers  = {},
} = {}) {
  const url = `${BASE_URL}${endpoint}`;
  const requestHeaders = { ...headers };

  if (auth && Auth.isLoggedIn()) {
    requestHeaders['Authorization'] = `Bearer ${Auth.getToken()}`;
  }

  if (!isForm && body !== null) {
    requestHeaders['Content-Type'] = 'application/json';
  }

  const config = { method, headers: requestHeaders };
  if (body !== null) {
    config.body = isForm ? body : JSON.stringify(body);
  }

  const response = await fetch(url, config);

  if (response.status === 204) return null;

  let data;
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  // Backend N-Tier format: { status, message, data }
  // Unwrap .data so callers get the raw payload
  if (typeof data === 'object' && data !== null && 'data' in data && 'status' in data) {
    data = data.data;
  }

  if (!response.ok) {
    const message =
      (typeof data === 'object' && (data?.title || data?.message || data?.error)) ||
      (typeof data === 'string' && data) ||
      `HTTP ${response.status}`;

    const error = new Error(message);
    error.status = response.status;
    error.data   = data;
    throw error;
  }

  return data;
}

const get      = (url, opts = {}) => request(url, { ...opts, method: 'GET'    });
const post     = (url, body, opts = {}) => request(url, { ...opts, method: 'POST', body });
const put      = (url, body, opts = {}) => request(url, { ...opts, method: 'PUT', body });
const del      = (url, opts = {}) => request(url, { ...opts, method: 'DELETE' });
const postForm = (url, formData, opts = {}) => request(url, { ...opts, method: 'POST', body: formData, isForm: true });

// ============================================================
//  API MODULES
// ============================================================

// ── Account / Auth ───────────────────────────────────────────
export const AccountApi = {
  register: (payload) =>
    post('/api/Account/register', payload, { auth: false }),

  login: async (payload) => {
    // Backend returns: { status, message, data: { token, role, user: { id, userName, email, fullName } } }
    // After unwrapping: { token, role, user: { id, userName, email, fullName } }
    const data = await post('/api/Account/login', payload, { auth: false });
    // Normalize for Auth.save() which expects: { token, role, user: { username } }
    Auth.save({
      token: data.token,
      role:  data.role,
      user:  { username: data.user?.userName || data.user?.username },
    });
    return data;
  },

  logout: () => Auth.clear(),
};

// ── Món Ăn ───────────────────────────────────────────────────
export const MonAnApi = {
  getAll:   () => get('/api/MonAn', { auth: false }),
  getById:  (id) => get(`/api/MonAn/${id}`, { auth: false }),
  create:   (formData) => postForm('/api/MonAn', formData),
  update:   (id, formData) => request(`/api/MonAn/${id}/with-image`, { method: 'PUT', body: formData, isForm: true }),
  delete:   (id) => del(`/api/MonAn/${id}`),
};

// ── Bàn Ăn ───────────────────────────────────────────────────
export const BanAnApi = {
  getAll: () => get('/api/BanAn'),
  create: (payload) => post('/api/BanAn', payload),
  update: (id, payload) => put(`/api/BanAn/${id}`, payload),
  delete: (id) => del(`/api/BanAn/${id}`),
  toggleStatus: (id) => post(`/api/BanAn/ToggleStatus/${id}`),
};

// ── Đặt Bàn ──────────────────────────────────────────────────
export const DatBanApi = {
  create:        (payload) => post('/api/DatBan/Create', payload),
  getMyHistory:  () => get('/api/DatBan/my-history'),
  getAll:        () => get('/api/DatBan'),
  cancel:        (id) => del(`/api/DatBan/cancel/${id}`),
  confirmPayment:(id) => post(`/api/DatBan/ConfirmPayment/${id}`),
  confirmBooking:(id) => post(`/api/DatBan/ConfirmBooking/${id}`),
  submitRating:  (payload) => post('/api/DatBan/CreateDanhGia', payload),
  cancelByAdmin: (id) => del(`/api/DatBan/cancel-by-admin/${id}`),
};

// ── Utilities ────────────────────────────────────────────────
export function getImageUrl(path, fallback = '/assets/placeholder-food.jpg') {
  if (!path) return fallback;
  if (path.startsWith('http')) return path;

  // Strip all leading path segments that might come from DB seeding
  // e.g. "wwwroot/Images/xxx.jpg", "Images/xxx.jpg", "/xxx.jpg" → "xxx.jpg"
  const clean = path
    .replace(/^wwwroot\/?/i, '')   // remove wwwroot/ prefix (Windows)
    .replace(/^wwwroot\/?/i, '')  // run twice to handle "wwwroot/wwwroot/..."
    .replace(/^\/?Images\/?/i, '') // remove Images/ prefix if present
    .replace(/^\/+/, '');          // remove any remaining leading slashes

  return `${BASE_URL}/frontend/Images/${clean}`;
}

export function formatPrice(amount) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency', currency: 'VND',
  }).format(amount);
}

export function formatDate(dateString) {
    if (!dateString) return "---";
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return "N/A";

    return new Intl.DateTimeFormat('vi-VN', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    }).format(date);
}


export const Toast = {
    _container: null,

    _getContainer() {
        if (!this._container) {
            this._container = document.querySelector('.toast-container');
            if (!this._container) {
                this._container = document.createElement('div');
                this._container.className = 'toast-container';
                document.body.appendChild(this._container);
            }
        }
        return this._container;
    },

    show(message, type = 'info', duration = 4000) {
        const icons = { success: '✓', error: '✕', info: 'ℹ' };
        const container = this._getContainer();

        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;

        toast.innerHTML = `
            <span style="font-size:1.1rem; font-weight:bold; flex-shrink:0">${icons[type]}</span>
            <span style="flex:1; line-height: 1.4">${message}</span>
        `;

        container.prepend(toast);
        void toast.offsetWidth; 
        toast.classList.add('active');

        // 2. ÉP BUỘC trình duyệt phải render Toast trước khi chạy code tiếp theo
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                toast.classList.add('active');
            });
        });

        // 3. Tự động đóng
        setTimeout(() => {
            toast.classList.remove('active');
            toast.addEventListener('transitionend', () => toast.remove(), { once: true });
        }, duration);
    },

    success: (msg) => Toast.show(msg, 'success'),
    error: (msg) => Toast.show(msg, 'error'),
    info: (msg) => Toast.show(msg, 'info'),
}; export default { Auth, Account: AccountApi, MonAn: MonAnApi, BanAn: BanAnApi, DatBan: DatBanApi, Toast, getImageUrl, formatPrice, formatDate };
