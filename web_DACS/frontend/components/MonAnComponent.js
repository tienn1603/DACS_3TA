// ============================================================
//  3TA RESTAURANT — MonAnComponent.js
//  Dual-mode component: User (browse) / Admin (CRUD)
//  Depends on: ../js/apiClient.js, ../js/services/authService.js, shared.css
// ============================================================

import { Auth as AuthService } from '../js/services/authService.js';
import { MonAnApi, Toast, getImageUrl, formatPrice } from '../js/apiClient.js';

// ============================================================
//  MonAnComponent  —  main export
//  Usage:
//    const comp = new MonAnComponent('#menu-root');
//    await comp.init();
//
//    // Admin mode: new MonAnComponent('#menu-root', { admin: true })
// ============================================================
export default class MonAnComponent {
  /**
   * @param {string|HTMLElement} container  — selector or element
   * @param {{ admin?: boolean }}  options
   */
  constructor(container, options = {}) {
    this.root    = typeof container === 'string'
      ? document.querySelector(container)
      : container;

    this.isAdmin = options.admin ?? AuthService.isAdmin();
    this.items   = [];
    this.filtered = [];
    this.search  = '';
    this.loading = false;
    this._editingId = null;
  }

  // ── Public API ──────────────────────────────────────────────
  async init() {
    this._renderShell();
    await this._loadItems();
  }

  async reload() {
    await this._loadItems();
  }

  // ── Data ────────────────────────────────────────────────────
  async _loadItems() {
    this._setLoading(true);
    try {
      // Map PascalCase + camelCase API fields → camelCase for consistent UI code
      this.items = (await MonAnApi.getAll()).map(item => ({
        id:       item.Id ?? item.id ?? 0,
        tenMonAn: item.TenMon ?? item.tenMon ?? item.tenMonAn ?? '',
        gia:      item.Gia    ?? item.gia    ?? 0,
        moTa:     item.MoTa   ?? item.moTa   ?? '',
        hinhAnh:  item.HinhAnh ?? item.hinhAnh ?? '',
        loai:     item.Loai   ?? item.loai   ?? '',
      }));
      this.filtered = [...this.items];
      this._renderList();
    } catch (err) {
      this._renderError(err.message);
    } finally {
      this._setLoading(false);
    }
  }

  _filterItems(query) {
    this.search = query.toLowerCase().trim();
    this.filtered = this.search
      ? this.items.filter(m =>
          m.tenMonAn.toLowerCase().includes(this.search) ||
          m.moTa?.toLowerCase().includes(this.search)
        )
      : [...this.items];
    this._renderList();
  }

  // ── Shell (toolbar + grid area) ──────────────────────────────
  _renderShell() {
    this.root.innerHTML = `
      <div class="mon-an-toolbar">
        <div class="mon-an-search-wrap">
          <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
          </svg>
          <input
            class="form-input mon-an-search"
            type="search"
            placeholder="Tìm kiếm món ăn…"
            autocomplete="off"
          />
        </div>
        ${this.isAdmin ? `
          <button class="btn btn-primary mon-an-add-btn">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M12 5v14M5 12h14"/>
            </svg>
            Thêm món mới
          </button>
        ` : ''}
      </div>

      <div class="mon-an-list grid-auto" id="mon-an-list"></div>
      <div class="mon-an-modal-root"></div>
    `;

    this._listEl  = this.root.querySelector('#mon-an-list');
    this._modalEl = this.root.querySelector('.mon-an-modal-root');

    // Bind toolbar events
    this.root.querySelector('.mon-an-search')
      .addEventListener('input', e => this._filterItems(e.target.value));

    if (this.isAdmin) {
      this.root.querySelector('.mon-an-add-btn')
        .addEventListener('click', () => this._openFormModal(null));
    }
  }

  // ── Loading state ────────────────────────────────────────────
  _setLoading(val) {
    this.loading = val;
    if (!this._listEl) return;
    if (val) {
      this._listEl.innerHTML = `
        <div class="loading-state" style="grid-column:1/-1">
          <div class="spinner"></div>
          <span>Đang tải thực đơn…</span>
        </div>`;
    }
  }

  _renderError(msg) {
    this._listEl.innerHTML = `
      <div class="empty-state" style="grid-column:1/-1">
        <div class="empty-state__icon">⚠️</div>
        <p style="color:var(--status-occupied)">${msg}</p>
        <button class="btn btn-ghost btn-sm" onclick="this.closest('.mon-an-list').dispatchEvent(new Event('retry'))">
          Thử lại
        </button>
      </div>`;
    this._listEl.addEventListener('retry', () => this._loadItems(), { once: true });
  }

  // ── List Render ──────────────────────────────────────────────
  _renderList() {
    if (!this.filtered.length) {
      this._listEl.innerHTML = `
        <div class="empty-state" style="grid-column:1/-1">
          <div class="empty-state__icon">🍽️</div>
          <p>Không tìm thấy món ăn nào</p>
        </div>`;
      return;
    }

    this._listEl.innerHTML = this.filtered
      .map(item => this._cardHTML(item))
      .join('');

    // Bind card events
    this._listEl.querySelectorAll('[data-action]').forEach(btn => {
      btn.addEventListener('click', e => {
        e.stopPropagation();
        const action = btn.dataset.action;
        const id     = parseInt(btn.dataset.id, 10);
        const item   = this.items.find(m => m.id === id);

        if (action === 'edit')   this._openFormModal(item);
        if (action === 'delete') this._confirmDelete(item);
        if (action === 'order')  this._dispatchOrder(item);
      });
    });
  }

  // ── Card HTML ────────────────────────────────────────────────
  _cardHTML(item) {
    // Defensive: ensure every field has a safe default
    const id       = item.id       ?? '';
    const tenMonAn = item.tenMonAn ?? item.TenMon ?? '';
    const moTa     = item.moTa     ?? item.MoTa ?? '';
    const gia      = item.gia      ?? item.Gia ?? '';
    const hinhAnh  = item.hinhAnh  ?? item.HinhAnh ?? '';

    const imgSrc = getImageUrl(hinhAnh);
    const price  = formatPrice(gia);
    const hasImg = hinhAnh && hinhAnh.trim() !== '';

    return `
      <div class="card ${this.isAdmin ? 'card--admin' : 'card--user'}" data-id="${id}">
        ${hasImg
          ? `<img class="card__image" src="${imgSrc}" alt="${tenMonAn}" loading="lazy" onerror="this.parentNode.replaceChild(Object.assign(document.createElement('div'),{className:'card__image-placeholder',innerHTML:'🍜'}),this)">`
          : `<div class="card__image-placeholder">🍜</div>`
        }

        ${this.isAdmin ? `
          <div class="card__actions">
            <button class="btn btn-ghost btn-icon" data-action="edit" data-id="${id}" title="Chỉnh sửa">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </button>
            <button class="btn btn-danger btn-icon" data-action="delete" data-id="${id}" title="Xoá">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/>
                <path d="M10 11v6M14 11v6"/><path d="M9 6V4h6v2"/>
              </svg>
            </button>
          </div>
        ` : ''}

        <div class="card__body">
          <h3 class="card__title">${tenMonAn}</h3>
          <p class="card__desc">${moTa}</p>
        </div>

        <div class="card__footer">
          <span class="card__price">${price}</span>
          ${!this.isAdmin ? `
            <button class="btn btn-primary btn-sm" data-action="order" data-id="${id}">
              Đặt món
            </button>
          ` : `
            <span style="font-size:var(--text-xs);color:var(--text-muted)">ID: ${id}</span>
          `}
        </div>
      </div>
    `;
  }

  // ── Form Modal (Create / Edit) ───────────────────────────────
  _openFormModal(item = null) {
    const isEdit = !!item;
    this._editingId = item?.id ?? null;

    this._modalEl.innerHTML = `
      <div class="modal-overlay" id="mon-an-modal">
        <div class="modal">
          <div class="modal__header">
            <h2 class="modal__title">${isEdit ? 'Chỉnh sửa món ăn' : 'Thêm món ăn mới'}</h2>
            <button class="modal__close" id="modal-close">✕</button>
          </div>

          <form id="mon-an-form" novalidate>
            <div class="form-group">
              <label class="form-label">Tên món ăn <span style="color:var(--status-occupied)">*</span></label>
              <input class="form-input" name="tenMonAn" type="text"
                placeholder="Ví dụ: Tôm hùm nướng" required
                value="${isEdit ? item.tenMonAn : ''}" />
              <span class="form-error" id="err-tenMonAn"></span>
            </div>

            <div class="form-group">
              <label class="form-label">Giá (VNĐ) <span style="color:var(--status-occupied)">*</span></label>
              <input class="form-input" name="gia" type="number" min="0"
                placeholder="Ví dụ: 250000" required
                value="${isEdit ? item.gia : ''}" />
              <span class="form-error" id="err-gia"></span>
            </div>

            <div class="form-group">
              <label class="form-label">Mô tả</label>
              <textarea class="form-textarea" name="moTa"
                placeholder="Mô tả ngắn về món ăn…">${isEdit ? (item.moTa || '') : ''}</textarea>
            </div>

            <div class="form-group">
              <label class="form-label">Hình ảnh ${isEdit ? '(để trống nếu không đổi)' : ''}</label>
              <input class="form-input" name="hinhAnh" type="file"
                accept="image/*"
                style="padding: var(--space-2) var(--space-3);" />
              ${isEdit && item.hinhAnh ? `
                <div style="margin-top:var(--space-2)">
                  <img src="${getImageUrl(item.hinhAnh)}" alt="Ảnh hiện tại"
                    style="height:80px;border-radius:var(--radius-md);object-fit:cover;border:1px solid var(--border-subtle)" />
                </div>
              ` : ''}
            </div>

            <div style="display:flex;gap:var(--space-3);justify-content:flex-end;margin-top:var(--space-6)">
              <button type="button" class="btn btn-ghost" id="modal-cancel">Huỷ</button>
              <button type="submit" class="btn btn-primary" id="modal-submit">
                <span id="submit-label">${isEdit ? 'Lưu thay đổi' : 'Thêm món'}</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    `;

    const overlay = this._modalEl.querySelector('#mon-an-modal');
    const form    = this._modalEl.querySelector('#mon-an-form');
    const close   = () => { this._modalEl.innerHTML = ''; this._editingId = null; };

    this._modalEl.querySelector('#modal-close').addEventListener('click', close);
    this._modalEl.querySelector('#modal-cancel').addEventListener('click', close);
    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });

    form.addEventListener('submit', e => this._handleFormSubmit(e, isEdit, close));
  }

  async _handleFormSubmit(e, isEdit, closeFn) {
    e.preventDefault();
    const form   = e.target;
    const submit = form.querySelector('#modal-submit');
    const label  = form.querySelector('#submit-label');

    // Validate
    const tenMonAn = form.tenMonAn.value.trim();
    const gia      = parseFloat(form.gia.value);
    let valid      = true;

    document.querySelectorAll('.form-error').forEach(el => el.textContent = '');

    if (!tenMonAn) {
      document.querySelector('#err-tenMonAn').textContent = 'Vui lòng nhập tên món ăn';
      valid = false;
    }
    if (!gia || gia <= 0) {
      document.querySelector('#err-gia').textContent = 'Vui lòng nhập giá hợp lệ';
      valid = false;
    }
    if (!valid) return;

    // Build FormData (backend dùng [FromForm] — field name phải khớp backend CreateMonAnRequest)
    const fd = new FormData();
    fd.append('TenMon', tenMonAn);
    fd.append('MoTa',   form.moTa.value.trim());
    fd.append('Gia',    gia);
    if (form.hinhAnh.files[0]) {
      fd.append('HinhAnh', form.hinhAnh.files[0]);
    }

    submit.disabled = true;
    label.textContent = 'Đang lưu…';

    try {
      if (isEdit) {
        await MonAnApi.update(this._editingId, fd);
        Toast.success('Cập nhật món ăn thành công!');
      } else {
        await MonAnApi.create(fd);
        Toast.success('Thêm món ăn thành công!');
      }
      closeFn();
      await this._loadItems();
    } catch (err) {
      Toast.error(err.message || 'Có lỗi xảy ra, thử lại sau!');
      submit.disabled = false;
      label.textContent = isEdit ? 'Lưu thay đổi' : 'Thêm món';
    }
  }

  // ── Delete Confirm ───────────────────────────────────────────
  _confirmDelete(item) {
    this._modalEl.innerHTML = `
      <div class="modal-overlay" id="confirm-modal">
        <div class="modal" style="max-width:400px;text-align:center">
          <div style="font-size:2.5rem;margin-bottom:var(--space-4)">🗑️</div>
          <h3 class="modal__title" style="margin-bottom:var(--space-3)">Xoá món ăn?</h3>
          <p style="color:var(--text-muted);font-size:var(--text-sm);margin-bottom:var(--space-8)">
            Bạn có chắc muốn xoá <strong style="color:var(--text-primary)">${item.tenMonAn}</strong>?<br/>
            Hành động này không thể hoàn tác.
          </p>
          <div style="display:flex;gap:var(--space-3);justify-content:center">
            <button class="btn btn-ghost" id="del-cancel">Huỷ</button>
            <button class="btn btn-danger" id="del-confirm">Xoá</button>
          </div>
        </div>
      </div>
    `;

    const overlay = this._modalEl.querySelector('#confirm-modal');
    const close   = () => { this._modalEl.innerHTML = ''; };

    this._modalEl.querySelector('#del-cancel').addEventListener('click', close);
    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });

    this._modalEl.querySelector('#del-confirm').addEventListener('click', async (e) => {
      e.target.disabled = true;
      e.target.textContent = 'Đang xoá…';
      try {
        await MonAnApi.delete(item.id);
        Toast.success(`Đã xoá "${item.tenMonAn}"`);
        close();
        await this._loadItems();
      } catch (err) {
        Toast.error(err.message || 'Không thể xoá món ăn này');
        close();
      }
    });
  }

  // ── Order Event (User mode) ───────────────────────────────────
  _dispatchOrder(item) {
    // Phát Custom Event lên DOM để trang chủ lắng nghe
    this.root.dispatchEvent(new CustomEvent('mon-an:order', {
      bubbles: true,
      detail: { item },
    }));
  }
}

// ============================================================
//  Inlined styles for MonAnComponent
//  (injected once into <head> when module is loaded)
// ============================================================
(function injectStyles() {
  if (document.getElementById('mon-an-styles')) return;
  const style = document.createElement('style');
  style.id = 'mon-an-styles';
  style.textContent = `
    .mon-an-toolbar {
      display: flex;
      align-items: center;
      gap: var(--space-4);
      margin-bottom: var(--space-8);
      flex-wrap: wrap;
    }

    .mon-an-search-wrap {
      position: relative;
      flex: 1;
      min-width: 220px;
    }

    .mon-an-search-wrap .search-icon {
      position: absolute;
      left: var(--space-4);
      top: 50%;
      transform: translateY(-50%);
      color: var(--text-muted);
      pointer-events: none;
    }

    .mon-an-search {
      padding-left: calc(var(--space-4) + 16px + var(--space-3));
    }

    /* Card user hover effect */
    .card--user {
      cursor: default;
    }

    .card--user .card__image {
      transition: transform var(--transition-slow);
    }

    .card--user:hover .card__image {
      transform: scale(1.04);
    }

    .card--user {
      overflow: hidden;
    }

    @media (max-width: 640px) {
      .mon-an-toolbar { flex-direction: column; align-items: stretch; }
      .mon-an-add-btn { width: 100%; justify-content: center; }
    }
  `;
  document.head.appendChild(style);
})();
