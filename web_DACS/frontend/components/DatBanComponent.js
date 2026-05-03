// ============================================================
//  3TA RESTAURANT — DatBanComponent.js
//  Admin reservation management: list + confirm/cancel/pay
//  Depends on: ../js/apiClient.js, shared.css
// ============================================================

import { DatBanApi, formatDate, formatPrice, Toast } from '../js/apiClient.js';

export default class DatBanComponent {
  constructor(container, options = {}) {
    this.root     = typeof container === 'string'
      ? document.querySelector(container)
      : container;
    this.isAdmin  = options.admin ?? false;
    this.items    = [];
    this.filtered = [];
    this.filter   = 'all';
    this._modalEl = null;
  }

  async init() { await this._loadItems(); }
  async reload() { await this._loadItems(); }

  async _loadItems() {
    this._setLoading(true);
    try {
      this.items = (await DatBanApi.getAll()).map(r => ({
        id:             r.id ?? r.Id ?? 0,
        tenKhachHang:   r.tenKhachHang ?? r.TenKhachHang ?? '',
        soDienThoai:    r.soDienThoai ?? r.SoDienThoai ?? '',
        ngayDat:        r.ngayDat ?? r.NgayDat ?? '',
        gioDenDuyKien:  r.gioDenDuyKien ?? r.GioDenDuyKien ?? r.ngayDat ?? '',
        banAnId:        r.banAnId ?? r.BanAnId ?? 0,
        soBan:          r.banAn?.soBan ?? r.BanAn?.SoBan ?? `Bàn ${r.banAnId ?? r.BanAnId ?? '?'}`,
        trangThai:      r.trangThai ?? r.TrangThai ?? 0,
        ghiChuGopBan:   r.ghiChuGopBan ?? r.GhiChuGopBan ?? '',
        chiTietDatMons: (r.chiTietDatMons ?? r.ChiTietDatMons ?? []).map(ct => ({
          id:       ct.id ?? ct.Id ?? 0,
          monAnId:  ct.monAnId ?? ct.MonAnId ?? 0,
          soLuong:  ct.soLuong ?? ct.SoLuong ?? 1,
          gia:      ct.monAn?.gia ?? ct.MonAn?.Gia ?? 0,
          tenMon:   ct.monAn?.tenMon ?? ct.MonAn?.TenMon ?? ct.monAn?.tenMonAn ?? '',
          hinhAnh:  ct.monAn?.hinhAnh ?? ct.MonAn?.HinhAnh ?? '',
        })),
        danhGia: Array.isArray(r.danhGias)
          ? (r.danhGias[0] ?? r.DanhGias?.[0] ?? null)
          : (r.danhGia ?? r.DanhGia ?? null),
      }));
      this._render();
    } catch (err) {
      this._setError(err.message);
    } finally {
      this._setLoading(false);
    }
  }

  _applyFilter() {
    const map = {
      all: null, pending: 0, confirmed: 1, occupied: 2,
      expired: 3, cancelled: -1, completed: 4,
    };
    const numeric = map[this.filter] ?? null;
    this.filtered = numeric === null
      ? [...this.items]
      : this.items.filter(r => r.trangThai === numeric);
    this._renderTable();
  }

  _render() {
    if (!this._modalEl) {
      this.root.innerHTML = `
        <div class="db-section-header">
          <div class="db-section-title">
  
          </div>
        </div>
        <div class="db-toolbar">
          <div id="datban-filter" class="db-filter">
            <button class="db-filter__chip active" data-filter="all">Tất cả</button>
            <button class="db-filter__chip" data-filter="pending">Chờ xác nhận</button>
            <button class="db-filter__chip" data-filter="confirmed">Đã xác nhận</button>
            <button class="db-filter__chip" data-filter="expired">Hết hạn</button>
            <button class="db-filter__chip" data-filter="cancelled">Đã hủy</button>
            <button class="db-filter__chip" data-filter="completed">Hoàn thành</button>
          </div>
        </div>
        <div class="db-table-wrap">
          <table class="db-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Khách hàng</th>
                <th>Bàn</th>
                <th>Ngày đặt</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody id="datban-tbody"></tbody>
          </table>
        </div>
      `;
      this._modalEl = document.createElement('div');
      this._modalEl.id = 'datban-modal-wrap';
      this.root.appendChild(this._modalEl);

      this.root.querySelector('#datban-filter').addEventListener('click', e => {
        const chip = e.target.closest('.db-filter__chip');
        if (!chip) return;
        this.root.querySelectorAll('.db-filter__chip')
          .forEach(c => c.classList.remove('active'));
        chip.classList.add('active');
        this.filter = chip.dataset.filter;
        this._applyFilter();
      });
    }
    this._applyFilter();
  }

  _renderTable() {
    const tbody = this.root.querySelector('#datban-tbody');

    if (!this.filtered.length) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6">
            <div class="empty-state" style="padding:var(--space-8)">
              <div class="empty-state__icon">📋</div>
              <p>Không có đơn nào</p>
            </div>
          </td>
        </tr>`;
      return;
    }

    const statusConfig = {
      0:   { label: 'Chờ xác nhận', bgColor: '#fef3c7', textColor: '#b45309' },
      1:   { label: 'Đã xác nhận',  bgColor: '#dbeafe', textColor: '#1d4ed8' },
      2:   { label: 'Đang dùng',    bgColor: '#ccfbf1', textColor: '#0f766e' },
      3:   { label: 'Hết hạn',       bgColor: '#f3f4f6', textColor: '#6b7280' },
      '-1': { label: 'Đã hủy',       bgColor: '#fee2e2', textColor: '#991b1b' },
      4:   { label: 'Hoàn thành',    bgColor: '#d1fae5', textColor: '#065f46' },
    };

    tbody.innerHTML = this.filtered.map(r => {
      const key = String(r.trangThai);
      const cfg = statusConfig[key] || { label: key, bgColor: '#f3f4f6', textColor: '#6b7280' };

      let actions = '—';

      if (this.isAdmin) {
        if (r.trangThai === 0) {
          actions = `
            <button class="btn btn-primary btn-sm" data-action="confirm" data-id="${r.id}">✓</button>
            <button class="btn btn-danger btn-sm" data-action="cancel-admin" data-id="${r.id}">Hủy</button>`;
        } else if (r.trangThai === 1) {
          actions = `
            <button class="btn btn-success btn-sm" data-action="pay" data-id="${r.id}">TT</button>
            <button class="btn btn-danger btn-sm" data-action="cancel-admin" data-id="${r.id}">Hủy</button>`;
        } else if (r.trangThai === 4) {
          actions = '—';
        }
      } else {
        if (r.trangThai === 0) {
          actions = `<button class="btn btn-danger btn-sm" data-action="cancel" data-id="${r.id}">Hủy</button>`;
        }
      }

      return `
        <tr data-id="${r.id}">
          <td style="font-family:var(--font-mono);font-size:var(--text-xs);color:var(--text-muted)">#${r.id}</td>
          <td>
            <div style="font-weight:500;color:var(--text-primary)">${r.tenKhachHang}</div>
            <div style="font-size:var(--text-xs);color:var(--text-muted)">${r.soDienThoai}</div>
          </td>
          <td>${r.soBan}</td>
          <td>${formatDate(r.ngayDat)}</td>
          <td>
            <span style="display:inline-block;padding:3px 10px;border-radius:12px;font-size:var(--text-xs);font-weight:600;
              color:${cfg.textColor};background:${cfg.bgColor}">
              ${cfg.label}
            </span>
          </td>
          <td>
            <div style="display:flex;gap:var(--space-2);align-items:center;flex-wrap:wrap">
              <button class="btn btn-ghost btn-sm" data-action="detail" data-id="${r.id}" title="Xem chi tiết">🔍</button>
              ${actions}
            </div>
          </td>
        </tr>`;
    }).join('');

    tbody.querySelectorAll('[data-action]').forEach(btn => {
      btn.addEventListener('click', () => {
        const id    = parseInt(btn.dataset.id, 10);
        const item  = this.items.find(r => r.id === id);
        const action = btn.dataset.action;
        if (action === 'confirm')       this._confirmBooking(item);
        if (action === 'pay')          this._confirmPayment(item);
        if (action === 'cancel')       this._confirmCancel(item);
        if (action === 'cancel-admin') this._confirmCancelAdmin(item);
        if (action === 'detail')       this._showDetail(item);
      });
    });
  }

  // ── Actions ─────────────────────────────────────────────────
  async _confirmBooking(item) {
    if (!confirm(`Xác nhận đơn đặt bàn #${item.id}?`)) return;
    try {
      await DatBanApi.confirmBooking(item.id);
      Toast.success('Đã xác nhận đặt bàn');
      await this._loadItems();
    } catch (err) {
      Toast.error(err.message || 'Lỗi xác nhận');
    }
  }

  async _confirmPayment(item) {
    if (!confirm(`Xác nhận thanh toán cho đơn #${item.id}?`)) return;
    try {
      await DatBanApi.confirmPayment(item.id);
      Toast.success('Đã thanh toán và giải phóng bàn');
      await this._loadItems();
      window.location.href = `reservation-detail.html?id=${item.id}`;
    } catch (err) {
      Toast.error(err.message || 'Lỗi thanh toán');
    }
  }

  async _confirmCancel(item) {
    if (!confirm(`Hủy đơn đặt bàn #${item.id}?`)) return;
    try {
      await DatBanApi.cancel(item.id);
      Toast.success('Đã hủy đơn');
      await this._loadItems();
    } catch (err) {
      Toast.error(err.message || 'Lỗi hủy đơn');
    }
  }

  async _confirmCancelAdmin(item) {
    if (!confirm(`Hủy đơn đặt bàn #${item.id} của "${item.tenKhachHang}"?\nBàn sẽ được giải phóng.`)) return;
    try {
      await DatBanApi.cancelByAdmin(item.id);
      Toast.success('Đã hủy đơn thành công');
      await this._loadItems();
    } catch (err) {
      Toast.error(err.message || 'Lỗi hủy đơn');
    }
  }

  _showDetail(item) {
    const chiTiet = item.chiTietDatMons || [];
    const total   = chiTiet.reduce((s, ct) => s + (ct.soLuong * ct.gia), 0);

    const chiTietHtml = chiTiet.length
      ? chiTiet.map(ct => `
          <div style="display:flex;justify-content:space-between;padding:var(--space-2) 0;border-bottom:1px solid var(--border-subtle)">
            <span style="color:var(--text-secondary)">${ct.tenMon} × ${ct.soLuong}</span>
            <span style="color:var(--accent-primary)">${formatPrice(ct.soLuong * ct.gia)}</span>
          </div>`).join('')
      : '<p style="color:var(--text-muted);font-size:var(--text-sm)">Không có món ăn kèm</p>';

    this._modalEl.innerHTML = `
      <div class="modal-overlay" id="detail-modal">
        <div class="modal" style="max-width:500px">
          <div class="modal__header">
            <h3 class="modal__title">Chi tiết đơn #${item.id}</h3>
            <button class="modal__close detail-close-btn">✕</button>
          </div>
          <div style="padding:var(--space-5);display:flex;flex-direction:column;gap:var(--space-4)">
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:var(--space-4)">
              <div>
                <div style="font-size:var(--text-xs);color:var(--text-muted);text-transform:uppercase;letter-spacing:0.1em">Khách hàng</div>
                <div style="font-size:var(--text-sm);margin-top:2px">${item.tenKhachHang}</div>
              </div>
              <div>
                <div style="font-size:var(--text-xs);color:var(--text-muted);text-transform:uppercase;letter-spacing:0.1em">SĐT</div>
                <div style="font-size:var(--text-sm);margin-top:2px">${item.soDienThoai}</div>
              </div>
              <div>
                <div style="font-size:var(--text-xs);color:var(--text-muted);text-transform:uppercase;letter-spacing:0.1em">Ngày đặt</div>
                <div style="font-size:var(--text-sm);margin-top:2px">${formatDate(item.ngayDat)}</div>
              </div>
              <div>
                <div style="font-size:var(--text-xs);color:var(--text-muted);text-transform:uppercase;letter-spacing:0.1em">Giờ đến</div>
                <div style="font-size:var(--text-sm);margin-top:2px">${formatDate(item.gioDenDuyKien)}</div>
              </div>
            </div>
            <div>
              <div style="font-size:var(--text-xs);color:var(--text-muted);text-transform:uppercase;letter-spacing:0.1em;margin-bottom:var(--space-3)">Món đã đặt</div>
              ${chiTietHtml}
            </div>
            ${chiTiet.length ? `
            <div style="display:flex;justify-content:space-between;align-items:center;padding-top:var(--space-3);border-top:2px solid var(--border-subtle)">
              <span style="font-weight:600;color:var(--text-primary)">Tổng cộng</span>
              <span style="font-family:var(--font-display);font-size:var(--text-xl);color:var(--accent-primary)">${formatPrice(total)}</span>
            </div>` : ''}
          </div>
        </div>
      </div>
    `;

    const modal = this._modalEl.querySelector('#detail-modal');
    modal.querySelector('.detail-close-btn').addEventListener('click', () => { this._modalEl.innerHTML = ''; });
    modal.addEventListener('click', e => { if (e.target === modal) this._modalEl.innerHTML = ''; });
  }

  _setLoading(val) {
    const tbody = this.root.querySelector('#datban-tbody');
    if (!tbody) return;
    if (val) {
      tbody.innerHTML = `<tr><td colspan="6"><div class="loading-state" style="padding:var(--space-8)"><div class="spinner"></div></div></td></tr>`;
    }
  }

  _setError(msg) {
    const tbody = this.root.querySelector('#datban-tbody');
    if (tbody) tbody.innerHTML = `<tr><td colspan="6" style="color:var(--status-occupied);padding:var(--space-6);text-align:center">⚠️ ${msg}</td></tr>`;
  }
}
