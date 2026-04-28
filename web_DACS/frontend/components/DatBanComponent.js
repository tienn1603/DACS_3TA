// ============================================================
//  3TA RESTAURANT — DatBanComponent.js
//  Admin reservation management: list + confirm/cancel/pay
//  Depends on: ../js/apiClient.js, shared.css
// ============================================================

import { DatBanApi, formatDate, Toast } from '../js/apiClient.js';

export default class DatBanComponent {
  /**
   * @param {string|HTMLElement} container  — selector or element
   */
  constructor(container) {
    this.root     = typeof container === 'string'
      ? document.querySelector(container)
      : container;
    this.items    = [];
    this.filtered = [];
    this.filter   = 'all';
    this._modalEl = null;
  }

  // ── Public API ──────────────────────────────────────────────
  async init() {
    await this._loadItems();
  }

  async reload() {
    await this._loadItems();
  }

  // ── Data ────────────────────────────────────────────────────
  async _loadItems() {
    this._setLoading(true);
    try {
      // API trả JSON camelCase (ApiResponse dùng CamelCase naming policy)
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
      }));
      this._render();
    } catch (err) {
      this._setError(err.message);
    } finally {
      this._setLoading(false);
    }
  }

  // ── Filter ──────────────────────────────────────────────────
  _applyFilter() {
    const map = { all: null, pending: 0, confirmed: 1, occupied: 2, expired: 3, cancelled: -1 };
    const numeric = map[this.filter] ?? null;
    this.filtered = numeric === null
      ? [...this.items]
      : this.items.filter(r => r.trangThai === numeric);
    this._renderTable();
  }

  // ── Full Render ─────────────────────────────────────────────
  _render() {
    if (!this._modalEl) {
      this.root.innerHTML = `
        <div class="datban-toolbar">
          <div id="datban-filter" class="filter-group">
            <button class="filter-chip active" data-filter="all">Tất cả</button>
            <button class="filter-chip" data-filter="pending">Chờ xác nhận</button>
            <button class="filter-chip" data-filter="confirmed">Đã xác nhận</button>
            <button class="filter-chip" data-filter="occupied">Đang dùng</button>
            <button class="filter-chip" data-filter="expired">Hết hạn</button>
            <button class="filter-chip" data-filter="cancelled">Đã hủy</button>
          </div>
        </div>
        <div class="table-responsive">
          <table class="datban-table">
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
        const chip = e.target.closest('.filter-chip');
        if (!chip) return;
        this.root.querySelectorAll('#datban-filter .filter-chip')
          .forEach(c => c.classList.remove('active'));
        chip.classList.add('active');
        this.filter = chip.dataset.filter;
        this._applyFilter();
      });
    }

    this._applyFilter();
  }

  // ── Table Render ────────────────────────────────────────────
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

    const badgeClass = { 0:'badge-pending', 1:'badge-confirmed', 2:'badge-reserved', 3:'badge-reserved', '-1':'badge-cancelled' };
    const label      = { 0:'Chờ xác nhận', 1:'Đã xác nhận', 2:'Đang dùng', 3:'Hết hạn', '-1':'Đã hủy' };

    tbody.innerHTML = this.filtered.map(r => `
      <tr data-id="${r.id}">
        <td style="font-family:var(--font-mono);font-size:var(--text-xs);color:var(--text-muted)">#${r.id}</td>
        <td>
          <div style="font-weight:500;color:var(--text-primary)">${r.tenKhachHang}</div>
          <div style="font-size:var(--text-xs);color:var(--text-muted)">${r.soDienThoai}</div>
        </td>
        <td>${r.soBan}</td>
        <td>${formatDate(r.ngayDat)}</td>
        <td><span class="badge ${badgeClass[r.trangThai] || 'badge-pending'}">${label[r.trangThai] || (r.trangThai ?? '?')}</span></td>
        <td>
          <div style="display:flex;gap:var(--space-2);align-items:center;flex-wrap:wrap">
            <a class="btn btn-ghost btn-sm" href="reservation-detail.html?id=${r.id}" title="Xem chi tiết">🔍</a>
            ${r.trangThai === 0 ? `
              <button class="btn btn-primary btn-sm" data-action="confirm" data-id="${r.id}" title="Xác nhận đặt bàn">✓</button>
              <button class="btn btn-danger btn-sm" data-action="cancel" data-id="${r.id}">Hủy</button>
            ` : r.trangThai === 1 ? `
              <button class="btn btn-success btn-sm" data-action="pay" data-id="${r.id}" title="Thanh toán">TT</button>
              <button class="btn btn-danger btn-sm" data-action="cancel" data-id="${r.id}">Hủy</button>
            ` : '—'}
          </div>
        </td>
      </tr>
    `).join('');

    tbody.querySelectorAll('[data-action]').forEach(btn => {
      btn.addEventListener('click', () => {
        const id   = parseInt(btn.dataset.id, 10);
        const item = this.items.find(r => r.id === id);
        if (btn.dataset.action === 'confirm') this._confirmBooking(item);
        if (btn.dataset.action === 'pay')     this._confirmPayment(item);
        if (btn.dataset.action === 'cancel')  this._confirmCancel(item);
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

  // ── Loading / Error ──────────────────────────────────────────
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
async function submitBooking() {
    const resDateInput = document.getElementById('res-date').value;

    const payload = {
        tenKhachHang: document.getElementById('res-name').value,
        soDienThoai: document.getElementById('res-phone').value,
        // PHẢI dùng toISOString để Backend không bị lỗi thời gian
        gioDenDuyKien: new Date(resDateInput).toISOString(),
        banAnId: selectedTableId,
        cartItems: currentCart
    };

    try {
        const res = await DatBanApi.create(payload);
        Toast.success("Đặt bàn thành công!");

        // QUAN TRỌNG: Phải gọi lại hàm load bàn để nó đổi màu/khóa bàn
        await loadTables();

    } catch (e) {
        Toast.error(e.message);
    }
}
// ── Component-level styles ─────────────────────────────────────
(function injectStyles() {
  if (document.getElementById('datban-component-styles')) return;
  const style = document.createElement('style');
  style.id = 'datban-component-styles';
  style.textContent = `
    .datban-toolbar { margin-bottom: var(--space-6); }
    .table-responsive { overflow-x: auto; }
    .datban-table { width: 100%; border-collapse: collapse; font-size: var(--text-sm); }
    .datban-table th {
      text-align: left; padding: var(--space-3) var(--space-4);
      border-bottom: 2px solid var(--border-subtle); color: var(--text-muted);
      font-weight: 500; font-size: var(--text-xs); text-transform: uppercase;
      letter-spacing: 0.05em; white-space: nowrap;
    }
    .datban-table td { padding: var(--space-4); border-bottom: 1px solid var(--border-subtle); vertical-align: middle; }
    .datban-table tr:hover td { background: rgba(255,255,255,0.02); }
    .badge-confirmed { background: #d1fae5; color: #065f46; }
    .badge-cancelled { background: #fee2e2; color: #991b1b; }
    .filter-group { display: flex; gap: var(--space-2); flex-wrap: wrap; }
    .filter-chip {
      padding: var(--space-1) var(--space-4); border-radius: 20px;
      border: 1px solid var(--border-subtle); background: transparent;
      color: var(--text-muted); font-size: var(--text-sm); cursor: pointer; transition: all 0.2s;
    }
    .filter-chip:hover { border-color: var(--primary); color: var(--primary); }
    .filter-chip.active { background: var(--primary); border-color: var(--primary); color: #fff; }
  `;
  document.head.appendChild(style);
})();
