// ============================================================
//  3TA RESTAURANT — apiClient.js
//  Centralized fetch wrapper | JWT auto-injection | .NET REST
// ============================================================

const BASE_URL = 'http://localhost:5188'; // ← đổi thành URL backend thực tế

// ── Token Helpers ────────────────────────────────────────────
export const Auth = {
  getToken:    ()          => localStorage.getItem('tta_token'),
  getRole:     ()          => localStorage.getItem('tta_role'),
  getUsername: ()          => localStorage.getItem('tta_username'),
  isLoggedIn:  ()          => !!localStorage.getItem('tta_token'),
  isAdmin:     ()          => localStorage.getItem('tta_role') === 'Admin',

  save({ token, role, username }) {
    localStorage.setItem('tta_token',    token);
    localStorage.setItem('tta_role',     role);
    localStorage.setItem('tta_username', username);
  },

  clear() {
    localStorage.removeItem('tta_token');
    localStorage.removeItem('tta_role');
    localStorage.removeItem('tta_username');
  },
};

// ── Core Request Builder ─────────────────────────────────────
async function request(endpoint, {
  method   = 'GET',
  body     = null,
  isForm   = false,   // true → dùng FormData (upload ảnh)
  auth     = true,    // false → bỏ qua JWT
  headers  = {},
} = {}) {
  const url = `${BASE_URL}${endpoint}`;

  const requestHeaders = { ...headers };

  // Đính kèm JWT tự động
  if (auth && Auth.isLoggedIn()) {
    requestHeaders['Authorization'] = `Bearer ${Auth.getToken()}`;
  }

  // Nếu không phải FormData → gửi JSON
  if (!isForm && body !== null) {
    requestHeaders['Content-Type'] = 'application/json';
  }

  const config = {
    method,
    headers: requestHeaders,
  };

  if (body !== null) {
    config.body = isForm ? body : JSON.stringify(body);
  }

  const response = await fetch(url, config);

  // Xử lý response
  if (response.status === 204) return null; // No Content

  let data;
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  if (!response.ok) {
    // Trích thông báo lỗi từ ASP.NET Core response
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

// ── Shorthand Methods ────────────────────────────────────────
const get    = (url, opts = {}) => request(url, { ...opts, method: 'GET'    });
const post   = (url, body, opts = {}) => request(url, { ...opts, method: 'POST',   body });
const put    = (url, body, opts = {}) => request(url, { ...opts, method: 'PUT',    body });
const del    = (url, opts = {}) => request(url, { ...opts, method: 'DELETE' });
const postForm = (url, formData, opts = {}) => request(url, { ...opts, method: 'POST', body: formData, isForm: true });

// ============================================================
//  API MODULES
// ============================================================

// ── Auth ─────────────────────────────────────────────────────
export const AccountApi = {
  /**
   * Đăng ký tài khoản
   * @param {{ username, email, password }} payload
   */
  register: (payload) =>
    post('/api/Account/register', payload, { auth: false }),

  /**
   * Đăng nhập
   * @param {{ username, password }} payload
   * @returns {{ token, role, username }}
   */
  login: async (payload) => {
    const data = await post('/api/Account/login', payload, { auth: false });
    Auth.save(data);
    return data;
  },

  logout: () => Auth.clear(),
};

// ── Món Ăn ───────────────────────────────────────────────────
export const MonAnApi = {
  /**
   * Lấy danh sách món ăn (Public)
   * @returns {MonAn[]}
   */
  getAll: () => get('/api/MonAn', { auth: false }),

  /**
   * Lấy chi tiết một món ăn
   * @param {number} id
   */
  getById: (id) => get(`/api/MonAn/${id}`, { auth: false }),

  /**
   * Tạo món ăn mới (Admin - multipart/form-data để upload ảnh)
   * @param {FormData} formData  — fields: tenMonAn, gia, moTa, hinhAnh (file)
   */
  create: (formData) => postForm('/api/MonAn', formData),

  /**
   * Cập nhật món ăn (Admin)
   * @param {number} id
   * @param {FormData} formData
   */
  update: (id, formData) =>
    request(`/api/MonAn/${id}`, { method: 'PUT', body: formData, isForm: true }),

  /**
   * Xoá món ăn (Admin)
   * @param {number} id
   */
  delete: (id) => del(`/api/MonAn/${id}`),
};

// ── Bàn Ăn ───────────────────────────────────────────────────
export const BanAnApi = {
  /**
   * Lấy danh sách tất cả bàn ăn
   * @returns {BanAn[]}
   */
  getAll: () => get('/api/BanAn'),

  /**
   * Cập nhật trạng thái bàn ăn (Admin)
   * @param {number} id
   * @param {{ soBan, soChoNgoi, trangThai }} payload
   */
  update: (id, payload) => put(`/api/BanAn/${id}`, payload),
};

// ── Đặt Bàn ──────────────────────────────────────────────────
export const DatBanApi = {
  /**
   * Tạo đơn đặt bàn mới
   * @param {{ banAnId, ngayDat, chiTietDatMon: [{monAnId, soLuong}] }} payload
   */
  create: (payload) => post('/api/DatBan', payload),

  /**
   * Lấy lịch sử đặt bàn của user hiện tại
   * @returns {DatBan[]}
   */
  getMyHistory: () => get('/api/DatBan/my-history'),

  /**
   * Lấy tất cả đơn đặt bàn (Admin)
   * @returns {DatBan[]}
   */
  getAll: () => get('/api/DatBan'),

  /**
   * Hủy đơn đặt bàn
   * @param {number} id
   */
  cancel: (id) => del(`/api/DatBan/cancel/${id}`),

  /**
   * Admin xác nhận thanh toán
   * @param {{ datBanId }} payload
   */
  confirmPayment: (payload) => post('/api/DatBan/confirm-payment', payload),
};

// ── Image Helper ──────────────────────────────────────────────
/**
 * Trả về URL đầy đủ cho ảnh từ server
 * @param {string|null} path  — đường dẫn trả về từ API
 * @param {string} fallback   — ảnh mặc định nếu không có
 */
export function getImageUrl(path, fallback = '/assets/placeholder-food.jpg') {
  if (!path) return fallback;
  if (path.startsWith('http')) return path;
  return `${BASE_URL}/${path.replace(/^\//, '')}`;
}

/**
 * Format giá tiền sang VNĐ
 * @param {number} amount
 */
export function formatPrice(amount) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(amount);
}

/**
 * Format ngày giờ sang locale Việt Nam
 * @param {string|Date} date
 */
export function formatDate(date) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  }).format(new Date(date));
}

// ── Toast Notifications ───────────────────────────────────────
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

  show(message, type = 'info', duration = 3500) {
    const icons = { success: '✓', error: '✕', info: 'ℹ' };
    const container = this._getContainer();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
      <span style="font-size:1.1rem; flex-shrink:0">${icons[type]}</span>
      <span>${message}</span>
    `;
    container.appendChild(toast);

    setTimeout(() => {
      toast.style.animation = 'slideIn 0.3s ease reverse';
      toast.addEventListener('animationend', () => toast.remove());
    }, duration);
  },

  success: (msg) => Toast.show(msg, 'success'),
  error:   (msg) => Toast.show(msg, 'error'),
  info:    (msg) => Toast.show(msg, 'info'),
};

// Default export: tất cả API modules
export default {
  Auth,
  Account: AccountApi,
  MonAn:   MonAnApi,
  BanAn:   BanAnApi,
  DatBan:  DatBanApi,
  Toast,
  getImageUrl,
  formatPrice,
  formatDate,
};
