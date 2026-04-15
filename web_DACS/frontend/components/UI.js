
class UI extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' }); 
    }

    connectedCallback() {
        this.render();
    }

    render() {
        // Đường dẫn ảnh mới của bạn
        const imageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=800&q=80";

        this.shadowRoot.innerHTML = `
            <style>
                @import "/css/global.css"; /* Sử dụng CSS chung */

                .hero {
                    /* --- ĐÂY LÀ CHỖ CẦN THAY ĐỔI --- */
                    /* Sử dụng linear-gradient để tạo lớp phủ tối, giúp chữ trắng nổi bật hơn */
                    background-image: linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url('${imageUrl}');
                    
                    background-size: cover;
                    background-position: center;
                    height: 100vh;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: white;
                    text-align: center;
                }
                
                .hero-content {
                    max-width: 800px;
                    padding: 20px;
                }
                
                h1 {
                    font-size: 5rem;
                    margin: 20px 0;
                    font-weight: bold;
                }

                .badge {
                    background: #f1c40f; /* Màu vàng từ giao diện cũ */
                    color: black;
                    padding: 5px 15px;
                    border-radius: 20px;
                    font-weight: bold;
                    display: inline-block;
                    margin-bottom: 20px;
                }

                p {
                    font-size: 1.2rem;
                    margin-bottom: 30px;
                    font-style: italic;
                }

                .actions {
                    display: flex;
                    gap: 15px;
                    justify-content: center;
                }
            </style>

            <section class="hero">
                <div class="hero-content">
                    <span class="badge">⚓ CHÀO MỪNG ĐẾN VỚI NHÀ HÀNG</span>
                    <h1>Nhà hàng của nhóm 10</h1>
                    <p>"Tinh hoa từ đại dương - Tươi ngon trên từng bàn tiệc..."</p>
                    <div class="actions">
                        <button class="btn-red">📅 ĐẶT BÀN NGAY</button>
                        <button class="btn-outline">📖 XEM THỰC ĐƠN</button>
                    </div>
                </div>
            </section>
        `;
    }
}

customElements.define('app-hero', UI);