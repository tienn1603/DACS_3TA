
const BASE_URL = "https://localhost:7255/api"; 

export const API = {
    async getBanAns() {
        try {
            const res = await fetch(`${BASE_URL}/BanAn`);
            if (!res.ok) throw new Error("Lỗi kết nối API");
            return await res.json();
        } catch (error) {
            console.error(error);
            return []; 
        }
    }
};