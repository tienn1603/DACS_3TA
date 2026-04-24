// ============================================================
//  3TA RESTAURANT — ReservationDetailComponent.js
//  Chi tiết đặt bàn: header, timeline, dishes, actions
//  Depends on: ../js/apiClient.js, shared.css
// ============================================================

import { Auth as AuthService } from '../js/services/authService.js';
import { DatBanApi, formatDate, formatPrice, getImageUrl, Toast } from '../js/apiClient.js';

export default class ReservationDetailComponent {
  /**
   * @param {string|HTMLElement} container — selector or element
   * @param {{ id?: number }} options
   */
  constructor(container, options = {}) {
    this.root     = typeof container === 'string'
      ? document.querySelector(container)
      : container;
    this.id       = options.id ?? parseInt(new URLSearchParams(location.search).get('id'), 10);
    this.res      = null;       // normalized reservation data
    this.isAdmin  = AuthService.isAdmin();
    this.loading  = true;
    this.error    = null;
  }

  // ── Public API ──────────────────────────────────────────────
  async init() {
    this._showSkeleton();
    try {
      const raw = await this._fetch();
      this.res = this._normalize(raw);
      this._render();
    } catch (err) {
      this._showError(err.status || 500, err.message || 'Lỗi khi tải dữ liệu');
    } finally {
      this.loading = false;
    }
  }

  // ── Data fetch ─────────────────────────────────────────────
  async _fetch() {
    if (this.isAdmin) {
      const all = await DatBanApi.getAll();
      const found = all.find(r => (r.id ?? r.Id) === this.id);
      if (!found) throw Object.assign(new Error(`Không tìm thấy đơn đặt bàn #${this.id}`), { status: 404 });
      return found;
    } else {
      const history = await DatBanApi.getMyHistory();
      const found = history.find(r => (r.id ?? r.Id) === this.id);
      if (!found) throw Object.assign(new Error(`Không tìm thấy đơn đặt bàn #${this.id}`), { status: 404 });
      return found;
    }
  }

  // ── Normalize raw API data → consistent camelCase ───────────
  _normalize(r) {
    const dishes = (r.chiTietDatMons ?? r.ChiTietDatMons ?? []).map(ct => ({
      id:       ct.id ?? ct.Id ?? 0,
      monAnId:  ct.monAnId ?? ct.MonAnId ?? 0,
      soLuong:  ct.soLuong ?? ct.SoLuong ?? 1,
      gia:      ct.monAn?.gia ?? ct.MonAn?.Gia ?? 0,
      tenMon:   ct.monAn?.tenMon ?? ct.MonAn?.TenMon ?? ct.monAn?.tenMonAn ?? '',
      hinhAnh:  ct.monAn?.hinhAnh ?? ct.MonAn?.HinhAnh ?? '',
    }));

    const itemCount = dishes.reduce((s, d) => s + d.soLuong, 0);
    const total     = dishes.reduce((s, d) => s + d.gia * d.soLuong, 0);

    return {
      id:             r.id ?? r.Id ?? 0,
      tenKhachHang:   r.tenKhachHang ?? r.TenKhachHang ?? '',
      soDienThoai:    r.soDienThoai ?? r.SoDienThoai ?? '',
      ngayDat:        r.ngayDat ?? r.NgayDat ?? '',
      gioDenDuyKien:  r.gioDenDuyKien ?? r.GioDenDuyKien ?? r.ngayDat ?? '',
      banAnId:        r.banAnId ?? r.BanAnId ?? 0,
      soBan:          r.banAn?.soBan ?? r.BanAn?.SoBan ?? `Bàn ${r.banAnId ?? r.BanAnId ?? '?'}`,
      trangThai:      r.trangThai ?? r.TrangThai ?? 0,
      ghiChuGopBan:   r.ghiChuGopBan ?? r.GhiChuGopBan ?? '',
      dishes,
      itemCount,
      total,
    };
  }

  // ── Skeleton UI ─────────────────────────────────────────────
  _showSkeleton() {
    this.root.innerHTML = `
      <div style="max-width:900px;margin:0 auto;padding:0 var(--space-8)">
        <div class="skeleton" style="height:20px;width:260px;margin-bottom:var(--space-8)"></div>
        <div class="skeleton" style="height:200px;margin-bottom:var(--space-6);border-radius:var(--radius-xl)"></div>
        <div class="skeleton" style="height:80px;margin-bottom:var(--space-6);border-radius:var(--radius-xl)"></div>
        <div class="skeleton" style="height:320px;border-radius:var(--radius-xl)"></div>
      </div>`;
  }

  // ── Error UI ────────────────────────────────────────────────
  _showError(code, msg) {
    this.root.innerHTML = `
      <div style="max-width:900px;margin:0 auto;padding:var(--space-20) var(--space-8);text-align:center">
        <div style="font-family:var(--font-display);font-size:6rem;font-weight:300;color:var(--border-active);line-height:1">${code}</div>
        <div style="font-size:var(--text-lg);color:var(--text-muted);margin-top:var(--space-4)">${msg}</div>
        <div style="margin-top:var(--space-8);display:flex;gap:var(--space-4);justify-content:center">
          <a href="profile.html" class="btn btn-secondary">← Lịch sử đặt bàn</a>
          <a href="index.html"   class="btn btn-ghost">Trang chủ</a>
        </div>
      </div>`;
  }

  // ── Main Render ─────────────────────────────────────────────
  _render() {
    const r = this.res;
    const st = r.trangThai;   // 0=pending, 1=confirmed, 2=occupied, 3=expired, -1=cancelled

    const statusCfg = {
      0: { label: 'Chờ xác nhận', badge: 'badge-pending',   icon: '⏳' },
      1: { label: 'Đã xác nhận',  badge: 'badge-confirmed', icon: '✅' },
      2: { label: 'Đang dùng',    badge: 'badge-reserved',  icon: '🍽️' },
      3: { label: 'Hết hạn',      badge: 'badge-reserved',  icon: '⏰' },
     '-1': { label: 'Đã hủy',      badge: 'badge-cancelled', icon: '✕'  },
    };
    const cfg = statusCfg[st] ?? statusCfg[0];

    // Timeline steps
    const isCancelled = st === -1;
    const isPending   = st === 0;
    const isConfirmed = st === 1;
    const isOccupied  = st === 2;

    const steps = isCancelled
      ? [
          { label: 'Đặt bàn',  done: true,  active: false, cancelled: false },
          { label: 'Đã hủy',   done: false, active: true,  cancelled: true  },
        ]
      : [
          { label: 'Đặt bàn',       done: true,                         active: false },
          { label: 'Chờ xác nhận',  done: isConfirmed || isOccupied,    active: isPending },
          { label: 'Đã xác nhận',    done: isOccupied,                  active: isConfirmed },
          { label: 'Thanh toán',      done: false,                       active: isOccupied },
        ];

    const timelineHTML = steps.map(s => `
      <div class="tl-step ${s.done ? 'done' : ''} ${s.active ? 'active' : ''} ${s.cancelled ? 'cancelled' : ''}">
        <div class="tl-dot">${s.done ? '✓' : s.cancelled ? '✕' : ''}</div>
        <div class="tl-label">${s.label}</div>
      </div>`).join('');

    // Dishes
    const dishesHTML = r.dishes.length
      ? r.dishes.map(d => {
          const imgSrc = d.hinhAnh ? getImageUrl(d.hinhAnh) : null;
          return `
            <div class="dish-item">
              <div class="dish-item__img">
                ${imgSrc
                  ? `<img src="${imgSrc}" alt="${d.tenMon}" onerror="this.parentNode.innerHTML='🍜'" />`
                  : '🍜'}
              </div>
              <div class="dish-item__info">
                <div class="dish-item__name">${d.tenMon}</div>
                <div class="dish-item__unit">${formatPrice(d.gia)} / phần</div>
              </div>
              <div class="dish-item__qty-col">
                <div class="qty-badge">${d.soLuong}</div>
              </div>
              <div class="dish-item__unit-price">${formatPrice(d.gia)} × ${d.soLuong}</div>
              <div class="dish-item__subtotal">${formatPrice(d.gia * d.soLuong)}</div>
            </div>`;
        }).join('')
      : `<div style="padding:var(--space-10);text-align:center;color:var(--text-muted)">
           <div style="font-size:2.5rem;opacity:.35;margin-bottom:var(--space-3)">🍽️</div>
           <p>Không có thông tin món ăn</p>
         </div>`;

    // Admin actions
    const adminActionsHTML = this.isAdmin && (st === 0 || st === 1) ? `
      <button class="btn btn-primary" id="rdc-confirm-pay">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M20 12V22H4V12"/><path d="M22 7H2v5h20V7z"/>
          <path d="M12 22V7"/><path d="M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7z"/>
          <path d="M12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7z"/>
        </svg>
        ${st === 0 ? 'Xác nhận đặt bàn' : 'Xác nhận thanh toán'}
      </button>` : '';

    // Cancel button
    const cancelHTML = (st === 0 || st === 1) ? `
      <button class="btn btn-danger" id="rdc-cancel">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"/>
          <line x1="15" y1="9" x2="9" y2="15"/>
          <line x1="9" y1="9" x2="15" y2="15"/>
        </svg>
        Hủy đặt bàn
      </button>` : '';

    // Breadcrumb
    const breadcrumbHTML = `
      <nav class="breadcrumb">
        <a href="index.html">Trang chủ</a>
        <span class="breadcrumb__sep">›</span>
        <a href="profile.html">Tài khoản</a>
        <span class="breadcrumb__sep">›</span>
        <span class="breadcrumb__current">Đơn #${r.id}</span>
      </nav>`;

    // Order header
    const orderHeaderHTML = `
      <div class="order-header">
        <div class="order-header__top">
          <div>
            <div class="order-id-wrap">
              <span class="order-id-badge">ĐẶT BÀN #${String(r.id).padStart(5, '0')}</span>
            </div>
            <h1 class="order-title" style="margin-top:var(--space-3)">
              Bàn <em>${r.soBan}</em>
            </h1>
          </div>
          <div class="order-status-wrap">
            <span class="badge ${cfg.badge}" style="font-size:var(--text-sm);padding:var(--space-2) var(--space-5)">
              ${cfg.icon} ${cfg.label}
            </span>
            <span style="font-size:var(--text-xs);color:var(--text-muted)">${formatDate(r.ngayDat)}</span>
          </div>
        </div>

        <div class="order-info-grid">
          <div class="info-cell">
            <div class="info-cell__label">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
                <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
              </svg>
              Ngày đặt
            </div>
            <div class="info-cell__value">${formatDate(r.ngayDat)}</div>
          </div>
          <div class="info-cell">
            <div class="info-cell__label">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 9V7a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v2"/>
                <path d="M2 11v5a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-5a2 2 0 0 0-4 0v1H6v-1a2 2 0 0 0-4 0Z"/>
              </svg>
              Số bàn
            </div>
            <div class="info-cell__value">${r.soBan}</div>
          </div>
          <div class="info-cell">
            <div class="info-cell__label">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
                <line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/>
                <line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>
              </svg>
              Số món
            </div>
            <div class="info-cell__value">${r.itemCount} phần</div>
          </div>
          <div class="info-cell">
            <div class="info-cell__label">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="1" x2="12" y2="23"/>
                <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
              </svg>
              Tổng tiền
            </div>
            <div class="info-cell__value info-cell__value--accent">${formatPrice(r.total)}</div>
          </div>
        </div>
      </div>`;

    // Timeline card
    const timelineCardHTML = `
      <div class="timeline-card">
        <div class="timeline-card__title">Tiến trình đơn hàng</div>
        <div class="timeline">${timelineHTML}</div>
      </div>`;

    // Dishes card
    const dishesCardHTML = `
      <div class="dishes-card">
        <div class="dishes-card__header">
          <h2 class="dishes-card__title">Danh sách món ăn</h2>
          <span class="dishes-card__count">${r.dishes.length} loại · ${r.itemCount} phần</span>
        </div>
        ${dishesHTML}
        <div class="dishes-footer">
          <div class="summary-row">
            <span class="summary-row__label">Tạm tính</span>
            <span class="summary-row__value" style="color:var(--text-secondary)">${formatPrice(r.total)}</span>
          </div>
          <div class="summary-row">
            <span class="summary-row__label">Phí dịch vụ</span>
            <span class="summary-row__value" style="color:var(--status-available)">Miễn phí</span>
          </div>
          <div class="summary-row summary-row--total">
            <span class="summary-row__label">Tổng thanh toán</span>
            <span class="summary-row__value">${formatPrice(r.total)}</span>
          </div>
        </div>
      </div>`;

    // Action bar
    const actionBarHTML = `
      <div class="action-bar">
        <a href="profile.html" class="btn btn-ghost">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="19" y1="12" x2="5" y2="12"/>
            <polyline points="12 19 5 12 12 5"/>
          </svg>
          Quay lại
        </a>
        <div class="action-bar__right">
          <button class="btn btn-ghost" onclick="window.print()">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="6 9 6 2 18 2 18 9"/>
              <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/>
              <rect x="6" y="14" width="12" height="8"/>
            </svg>
            In hóa đơn
          </button>
          ${adminActionsHTML}
          ${cancelHTML}
        </div>
      </div>`;

    // Assemble
    this.root.innerHTML = `
      <div style="max-width:900px;margin:0 auto;padding:0 var(--space-8)">
        ${breadcrumbHTML}
        ${orderHeaderHTML}
        ${timelineCardHTML}
        ${dishesCardHTML}
        ${actionBarHTML}
      </div>`;

    // Bind action buttons
    this._bindActions();
  }

  // ── Button handlers ─────────────────────────────────────────
  _bindActions() {
    document.getElementById('rdc-confirm-pay')?.addEventListener('click', () => this._handleConfirmPay());
    document.getElementById('rdc-cancel')?.addEventListener('click', () => this._handleCancel());
  }

  async _handleConfirmPay() {
    const label = this.res.trangThai === 0 ? 'Xác nhận đặt bàn' : 'Xác nhận thanh toán';
    if (!confirm(`${label} đơn #${this.res.id}?`)) return;

    const btn = document.getElementById('rdc-confirm-pay');
    btn.disabled = true;
    btn.innerHTML = `<span class="spinner" style="width:16px;height:16px;border-width:2px"></span> Đang xử lý…`;

    try {
      if (this.res.trangThai === 0) {
        await DatBanApi.confirmBooking(this.res.id);
        Toast.success('Xác nhận đặt bàn thành công!');
      } else {
        await DatBanApi.confirmPayment(this.res.id);
        Toast.success('Xác nhận thanh toán thành công!');
      }
      setTimeout(() => this.init(), 600);
    } catch (err) {
      Toast.error(err.message || 'Thao tác thất bại');
      btn.disabled = false;
      btn.innerHTML = `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M20 12V22H4V12"/><path d="M22 7H2v5h20V7z"/>
        <path d="M12 22V7"/><path d="M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7z"/>
        <path d="M12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7z"/>
      </svg> ${label}`;
    }
  }

  async _handleCancel() {
    if (!confirm(`Hủy đơn đặt bàn #${this.res.id}? Hành động này không thể hoàn tác.`)) return;

    const btn = document.getElementById('rdc-cancel');
    btn.disabled = true; btn.textContent = 'Đang hủy…';

    try {
      await DatBanApi.cancel(this.res.id);
      Toast.success('Đã hủy đặt bàn');
      setTimeout(() => this.init(), 600);
    } catch (err) {
      Toast.error(err.message || 'Không thể hủy đơn này');
      btn.disabled = false; btn.textContent = 'Hủy đặt bàn';
    }
  }
}
