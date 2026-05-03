// ============================================================
//  3TA RESTAURANT — BanAnComponent.js
//  Admin table management: CRUD + status filter
//  Depends on: ../js/apiClient.js, shared.css
// ============================================================

import { BanAnApi, Toast } from '../js/apiClient.js';

export default class BanAnComponent {
  /**
   * @param {string|HTMLElement} container  — selector or element
   */
  constructor(container) {
    this.root      = typeof container === 'string'
      ? document.querySelector(container)
      : container;
    this.items     = [];
    this.filtered  = [];
    this.filter    = 'all';
    this._modalEl  = null;
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
      this.items = (await BanAnApi.getAll()).map(t => ({
        id:         t.Id ?? t.id ?? 0,
        soBan:      t.SoBan ?? t.soBan ?? '',
        soChoNgoi:  t.SoChoNgoi ?? t.soChoNgoi ?? 0,
        trangThai:  t.TrangThai ?? t.trangThai ?? 0,
      }));
      this._render();
    } catch (err) {
      this._setError(err.message);
    } finally {
      this._setLoading(false);
    }
  }

  // ── Filter ─────────────────────────────────────────────────
  _applyFilter() {
    const map = { all: null, available: 0, occupied: 1, reserved: 2 };
    const numeric = map[this.filter] ?? null;
    this.filtered = numeric === null
      ? [...this.items]
      : this.items.filter(t => t.trangThai === numeric);
    this._renderGrid();
  }

  // ── Full Render ─────────────────────────────────────────────
  _render() {
    // Build the shell once; preserve modal on subsequent calls
    if (!this._modalEl) {
      this.root.innerHTML = `
        <div class="banan-toolbar">
          <div id="banan-filter" class="filter-group">
            <button class="filter-chip active" data-status="all">Tất cả</button>
            <button class="filter-chip" data-status="available">Trống</button>
            <button class="filter-chip" data-status="occupied">Đang dùng</button>
            <button class="filter-chip" data-status="reserved">Đã đặt</button>
          </div>
          <button class="btn btn-primary" id="btn-add-table">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M12 5v14M5 12h14"/>
            </svg>
            Thêm bàn
          </button>
        </div>
        <div class="banan-list grid-auto" id="banan-grid"></div>
      `;
      this._modalEl   = document.createElement('div');
      this._modalEl.id = 'banan-modal-wrap';
      this.root.appendChild(this._modalEl);

      // Filter events
      this.root.querySelector('#banan-filter').addEventListener('click', e => {
        const chip = e.target.closest('.filter-chip');
        if (!chip) return;
        this.root.querySelectorAll('#banan-filter .filter-chip')
          .forEach(c => c.classList.remove('active'));
        chip.classList.add('active');
        this.filter = chip.dataset.status;
        this._applyFilter();
      });

      // Add button
      this.root.querySelector('#btn-add-table')
        .addEventListener('click', () => this._openModal(null));
    }

    this._applyFilter();
  }

  // ── Grid Render ─────────────────────────────────────────────
  _renderGrid() {
    const grid = this.root.querySelector('#banan-grid');
    if (!this.filtered.length) {
      grid.innerHTML = `
        <div class="empty-state" style="grid-column:1/-1">
          <div class="empty-state__icon">🪑</div>
          <p>Không có bàn nào</p>
        </div>`;
      return;
    }

    const label = { 0: 'Trống', 1: 'Đang dùng', 2: 'Đã đặt' };
    const badge = { 0: 'badge-available', 1: 'badge-occupied', 2: 'badge-reserved' };
    const cls   = { 0: 'available', 1: 'occupied', 2: 'reserved' };

    grid.innerHTML = this.filtered.map(t => `
      <div class="banan-card banan-card--${cls[t.trangThai] ?? 'available'}" data-id="${t.id}">
        <div class="banan-card__num">${t.soBan}</div>
        <div class="banan-card__seats">${t.soChoNgoi} chỗ ngồi</div>
        <span class="badge ${badge[t.trangThai] || 'badge-available'}">${label[t.trangThai] ?? (t.trangThai ?? '?')}</span>
        <div style="display:flex;gap:var(--space-2);margin-top:var(--space-3)">
          <button class="btn btn-ghost btn-sm" style="flex:1;font-size:11px" data-action="edit" data-id="${t.id}">Sửa</button>
          <button class="btn btn-danger btn-sm" style="font-size:11px" data-action="delete" data-id="${t.id}" title="Xóa bàn">✕</button>
        </div>
      </div>
    `).join('');

    grid.querySelectorAll('[data-action]').forEach(btn => {
      btn.addEventListener('click', e => {
        e.stopPropagation();
        const id   = parseInt(btn.dataset.id, 10);
        const item = this.items.find(t => t.id === id);
        if (btn.dataset.action === 'edit')   this._openModal(item);
        if (btn.dataset.action === 'delete') this._confirmDelete(item);
      });
    });
  }

  // ── Modal ───────────────────────────────────────────────────
  _openModal(item = null) {
    const isNew = !item;

    this._modalEl.innerHTML = `
      <div class="modal-overlay banan-modal-overlay">
        <div class="modal" style="max-width:420px">
          <div class="modal__header">
            <h2 class="modal__title">${isNew ? 'Thêm bàn ăn' : 'Cập nhật bàn ăn'}</h2>
            <button class="modal__close" id="banan-modal-close">✕</button>
          </div>
          <div class="modal__body">
            <div class="form-group">
              <label class="form-label">Số bàn</label>
              <input class="form-input" id="edit-soban" type="text"
                value="${isNew ? '' : (item.soBan ?? '')}"
                placeholder="VD: B01, B-3" required />
              <span class="form-error" id="err-soban"></span>
            </div>
            <div class="form-group">
              <label class="form-label">Số chỗ ngồi</label>
              <input class="form-input" id="edit-socho" type="number" min="1"
                value="${isNew ? '' : (item.soChoNgoi ?? '')}"
                placeholder="VD: 4" required />
              <span class="form-error" id="err-socho"></span>
            </div>
            ${isNew ? '' : `
            <div class="form-group">
              <label class="form-label">Trạng thái</label>
              <select class="form-select" id="edit-trangthai">
                <option value="0" ${item.trangThai === 0 ? 'selected' : ''}>Trống</option>
                <option value="1" ${item.trangThai === 1 ? 'selected' : ''}>Đang dùng</option>
                <option value="2" ${item.trangThai === 2 ? 'selected' : ''}>Đã đặt</option>
              </select>
            </div>
            `}
          </div>
          <div style="display:flex;gap:var(--space-3);justify-content:flex-end;padding:var(--space-4);border-top:1px solid var(--border-subtle)">
            <button class="btn btn-ghost" id="banan-modal-cancel">Huỷ</button>
            <button class="btn btn-primary" id="banan-modal-save">${isNew ? 'Thêm' : 'Lưu thay đổi'}</button>
          </div>
        </div>
      </div>
    `;

    const overlay = this._modalEl.querySelector('.modal-overlay');
    const close   = () => { this._modalEl.innerHTML = ''; };

    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
    this._modalEl.querySelector('#banan-modal-close').addEventListener('click', close);
    this._modalEl.querySelector('#banan-modal-cancel').addEventListener('click', close);

    this._modalEl.querySelector('#banan-modal-save').addEventListener('click', async () => {
      const btn = this._modalEl.querySelector('#banan-modal-save');
      btn.disabled = true; btn.textContent = 'Đang lưu…';

      // Validate
      const soBan = this._modalEl.querySelector('#edit-soban').value.trim();
      const soCho = parseInt(this._modalEl.querySelector('#edit-socho').value, 10);
      document.querySelectorAll('.form-error').forEach(el => el.textContent = '');
      let valid = true;
      if (!soBan) {
        this._modalEl.querySelector('#err-soban').textContent = 'Vui lòng nhập số bàn';
        valid = false;
      }
      if (!soCho || soCho < 1) {
        this._modalEl.querySelector('#err-socho').textContent = 'Vui lòng nhập số chỗ ngồi hợp lệ';
        valid = false;
      }
      if (!valid) { btn.disabled = false; btn.textContent = isNew ? 'Thêm' : 'Lưu thay đổi'; return; }

      try {
        if (isNew) {
          await BanAnApi.create({ SoBan: soBan, SoChoNgoi: soCho });
          Toast.success('Thêm bàn thành công');
        } else {
          await BanAnApi.update(item.id, {
            SoBan:     soBan,
            SoChoNgoi: soCho,
            TrangThai: parseInt(this._modalEl.querySelector('#edit-trangthai')?.value ?? '0', 10),
          });
          Toast.success('Cập nhật bàn thành công');
        }
        close();
        await this._loadItems();
      } catch (err) {
        Toast.error(err.message || 'Thao tác thất bại');
        btn.disabled = false; btn.textContent = isNew ? 'Thêm' : 'Lưu thay đổi';
      }
    });
  }

  // ── Delete Confirm ──────────────────────────────────────────
  _confirmDelete(item) {
    this._modalEl.innerHTML = `
      <div class="modal-overlay banan-modal-overlay">
        <div class="modal" style="max-width:380px;text-align:center">
          <div style="font-size:2.5rem;margin-bottom:var(--space-4)">🗑️</div>
          <h3 class="modal__title" style="margin-bottom:var(--space-3)">Xóa bàn ăn?</h3>
          <p style="color:var(--text-muted);font-size:var(--text-sm);margin-bottom:var(--space-8)">
            Bạn có chắc muốn xóa <strong style="color:var(--text-primary)">${item.soBan}</strong>?<br/>
            Hành động này không thể hoàn tác.
          </p>
          <div style="display:flex;gap:var(--space-3);justify-content:center">
            <button class="btn btn-ghost" id="del-cancel">Huỷ</button>
            <button class="btn btn-danger" id="del-confirm">Xóa</button>
          </div>
        </div>
      </div>
    `;

    const overlay = this._modalEl.querySelector('.modal-overlay');
    const close   = () => { this._modalEl.innerHTML = ''; };

    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
    this._modalEl.querySelector('#del-cancel').addEventListener('click', close);

    this._modalEl.querySelector('#del-confirm').addEventListener('click', async () => {
      const btn = this._modalEl.querySelector('#del-confirm');
      btn.disabled = true; btn.textContent = 'Đang xóa…';
      try {
        await BanAnApi.delete(item.id);
        Toast.success(`Đã xóa bàn "${item.soBan}"`);
        close();
        await this._loadItems();
      } catch (err) {
        Toast.error(err.message || 'Không thể xóa bàn này');
        btn.disabled = false; btn.textContent = 'Xóa';
      }
    });
  }

  // ── Loading / Error ──────────────────────────────────────────
  _setLoading(val) {
    const grid = this.root.querySelector('#banan-grid');
    if (!grid) return;
    if (val) {
      grid.innerHTML = `
        <div class="loading-state" style="grid-column:1/-1">
          <div class="spinner"></div><span>Đang tải bàn ăn…</span>
        </div>`;
    }
  }

  _setError(msg) {
    const grid = this.root.querySelector('#banan-grid');
    if (grid) grid.innerHTML = `
      <div class="empty-state" style="grid-column:1/-1">
        <div class="empty-state__icon">⚠️</div>
        <p style="color:var(--status-occupied)">${msg}</p>
        <button class="btn btn-ghost btn-sm" onclick="this.closest('.banan-list').dispatchEvent(new Event('retry'))">Thử lại</button>
      </div>`;
    this.root.querySelector('.banan-list')?.addEventListener('retry', () => this._loadItems(), { once: true });
  }
}

// ── Component-level styles ─────────────────────────────────────
(function injectStyles() {
  if (document.getElementById('banan-component-styles')) return;
  const style = document.createElement('style');
  style.id = 'banan-component-styles';
  style.textContent = `
    .banan-toolbar {
      display: flex;
      align-items: center;
      gap: var(--space-4);
      margin-bottom: var(--space-8);
      flex-wrap: wrap;
    }
    .filter-group { display: flex; gap: var(--space-2); flex-wrap: wrap; }
    .filter-chip {
      padding: var(--space-1) var(--space-4);
      border-radius: 20px;
      border: 1px solid var(--border-subtle);
      background: transparent;
      color: var(--text-muted);
      font-size: var(--text-sm);
      cursor: pointer;
      transition: all 0.2s;
    }
    .filter-chip:hover { border-color: var(--primary); color: var(--primary); }
    .filter-chip.active { background: var(--primary); border-color: var(--primary); color: #fff; }

    .banan-card {
      background: var(--bg-elevated);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      padding: var(--space-6);
      text-align: center;
      transition: box-shadow 0.2s;
    }
    .banan-card:hover { box-shadow: 0 4px 20px rgba(0,0,0,0.15); }
    .banan-card--available { border-left: 3px solid var(--status-available); }
    .banan-card--occupied  { border-left: 3px solid var(--status-occupied); }
    .banan-card--reserved  { border-left: 3px solid var(--status-reserved); }

    .banan-card__num {
      font-size: 2rem;
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: var(--space-1);
    }
    .banan-card__seats {
      font-size: var(--text-xs);
      color: var(--text-muted);
      margin-bottom: var(--space-3);
    }
    .banan-modal-overlay { align-items: center; }
  `;
  document.head.appendChild(style);
})();
