// ============================================================
//  3TA RESTAURANT — BanAnComponent.js (Updated with Timer)
//  Admin table management: CRUD + Time Tracking
// ============================================================

import { BanAnApi, Toast } from '../js/apiClient.js';

export default class BanAnComponent {
    constructor(container) {
        this.root = typeof container === 'string' ? document.querySelector(container) : container;
        this.items = [];
        this.filtered = [];
        this.filter = 'all';
        this._modalEl = null;
    }

    async init() { await this._loadItems(); }
    async reload() { await this._loadItems(); }

    async _loadItems() {
        this._setLoading(true);
        try {
            // Backend cần trả về thêm field TimeRange (ví dụ: "18:00 - 20:00")
            this.items = (await BanAnApi.getAll()).map(t => ({
                id: t.Id ?? t.id ?? 0,
                soBan: t.SoBan ?? t.soBan ?? '',
                soChoNgoi: t.SoChoNgoi ?? t.soChoNgoi ?? 0,
                trangThai: t.TrangThai ?? t.trangThai ?? 0,
                timeRange: t.TimeRange ?? t.timeRange ?? '' // Nhận thông tin từ Backend
            }));
            this._render();
        } catch (err) {
            this._setError(err.message);
        } finally {
            this._setLoading(false);
        }
    }

    _applyFilter() {
        const map = { all: null, available: 0, occupied: 1, reserved: 2 };
        const numeric = map[this.filter] ?? null;
        this.filtered = numeric === null ? [...this.items] : this.items.filter(t => t.trangThai === numeric);
        this._renderGrid();
    }

    _render() {
        if (!this._modalEl) {
            this.root.innerHTML = `
        <div class="banan-toolbar">
          <div id="banan-filter" class="filter-group">
            <button class="filter-chip active" data-status="all">Tất cả</button>
            <button class="filter-chip" data-status="available">Trống</button>
            <button class="filter-chip" data-status="occupied">Đang dùng</button>
            <button class="filter-chip" data-status="reserved">Đã đặt</button>
          </div>
          <button class="btn btn-primary" id="btn-add-table">Thêm bàn</button>
        </div>
        <div class="banan-list grid-auto" id="banan-grid"></div>
      `;
            this._modalEl = document.createElement('div');
            this._modalEl.id = 'banan-modal-wrap';
            this.root.appendChild(this._modalEl);

            this.root.querySelector('#banan-filter').addEventListener('click', e => {
                const chip = e.target.closest('.filter-chip');
                if (!chip) return;
                this.root.querySelectorAll('#banan-filter .filter-chip').forEach(c => c.classList.remove('active'));
                chip.classList.add('active');
                this.filter = chip.dataset.status;
                this._applyFilter();
            });

            this.root.querySelector('#btn-add-table').addEventListener('click', () => this._openModal(null));
        }
        this._applyFilter();
    }

    _renderGrid() {
        const grid = this.root.querySelector('#banan-grid');
        if (!this.filtered.length) {
            grid.innerHTML = `<div class="empty-state" style="grid-column:1/-1"><p>Không có bàn nào</p></div>`;
            return;
        }

        const label = { 0: 'Trống', 1: 'Đang dùng', 2: 'Đã đặt' };
        const badge = { 0: 'badge-available', 1: 'badge-occupied', 2: 'badge-reserved' };
        const cls = { 0: 'available', 1: 'occupied', 2: 'reserved' };

        grid.innerHTML = this.filtered.map(t => {
            const isLocked = t.trangThai !== 0;

            // Tạo khối HTML thời gian giống giao diện User
            const timerHtml = (isLocked && t.timeRange)
                ? `<div class="booking-time-slot">
             <div class="time-badge">
                <span class="icon">🕒</span>
                <span class="text">${t.timeRange}</span>
             </div>
             ${t.tenKhach ? `<div class="customer-name">👤 ${t.tenKhach}</div>` : ''}
           </div>`
                : '';

            return `
      <div class="banan-card banan-card--${cls[t.trangThai] ?? 'available'}" data-id="${t.id}">
        <div class="banan-card__num">${t.soBan}</div>
        <div class="banan-card__seats">👥 ${t.soChoNgoi} Chỗ</div>
        
        <span class="badge ${badge[t.trangThai]}">${label[t.trangThai]}</span>
        
        ${timerHtml} <div class="banan-card__actions">
          <button class="btn btn-ghost btn-sm" data-action="edit" data-id="${t.id}" ${isLocked ? 'disabled' : ''}>Sửa</button>
          <button class="btn btn-danger btn-sm" data-action="delete" data-id="${t.id}" ${isLocked ? 'disabled' : ''}>✕</button>
        </div>
      </div>`;
        }).join('');

        grid.querySelectorAll('[data-action]').forEach(btn => {
            btn.addEventListener('click', e => {
                e.stopPropagation();
                const id = parseInt(btn.dataset.id, 10);
                const item = this.items.find(t => t.id === id);
                if (btn.dataset.action === 'edit') this._openModal(item);
                if (btn.dataset.action === 'delete') this._confirmDelete(item);
            });
        });
    }

    // ... (Giữ nguyên các hàm _openModal, _confirmDelete, _setLoading như cũ) ...
    _openModal(item = null) { /* Nội dung modal của bạn */ }
    _confirmDelete(item) { /* Nội dung confirm delete của bạn */ }
    _setLoading(val) { /* Nội dung loading của bạn */ }
    _setError(msg) { /* Nội dung error của bạn */ }
}

// ── Bổ sung Style cho Timer ─────────────────────────────────────
(function injectAdminStyles() {
    if (document.getElementById('banan-timer-styles')) return;
    const style = document.createElement('style');
    style.id = 'banan-timer-styles';
    style.textContent = `
    .admin-timer-badge {
      margin-top: var(--space-3);
      padding: 4px 8px;
      background: rgba(42, 171, 184, 0.1);
      border: 1px dashed var(--color-wave);
      border-radius: var(--radius-md);
      font-size: 11px;
      color: var(--color-foam);
    }
    .banan-card--occupied .admin-timer-badge {
      border-color: var(--status-occupied);
      color: var(--status-occupied);
      background: rgba(224, 92, 92, 0.05);
    }
  `;
    document.head.appendChild(style);
})();