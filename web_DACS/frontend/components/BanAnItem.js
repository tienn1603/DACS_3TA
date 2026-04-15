import { API } from '../js/api.js';

class BanAnItem extends HTMLElement {
    async connectedCallback() {
        // Hiển thị trạng thái đang tải (Loading)
        this.innerHTML = `<p>Đang tải danh sách bàn...</p>`;
        
        const danhSachBan = await API.getBanAns();
        this.render(danhSachBan);
    }

    render(data) {
        if (!data || data.length === 0) {
            this.innerHTML = `<p>Không có dữ liệu bàn ăn hoặc lỗi Server.</p>`;
            return;
        }

        this.innerHTML = `
            <div class="ban-an-grid">
                ${data.map(ban => `
                    <div class="ban-card">
                        <h3>${ban.tenBan}</h3>
                        <p>Sức chứa: ${ban.sucChua} người</p>
                        <span class="status status-${ban.trangThai === 'Trống' ? 'available' : 'busy'}">
                            ${ban.trangThai}
                        </span>
                    </div>
                `).join('')}
            </div>
            <style>
                @import "/css/global.css";
                .ban-an-grid { 
                    display: grid; 
                    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); 
                    gap: 20px; 
                    padding: 20px;
                }
                .ban-card { 
                    border: 1px solid #ddd; 
                    padding: 20px; 
                    text-align: center; 
                    border-radius: 10px; 
                    background: white;
                    transition: transform 0.2s;
                }
                .ban-card:hover { transform: translateY(-5px); box-shadow: 0 5px 15px rgba(0,0,0,0.1); }
                .status-available { color: green; font-weight: bold; }
                .status-busy { color: red; font-weight: bold; }
            </style>
        `;
    }
}
customElements.define('ban-an-list', BanAnItem);