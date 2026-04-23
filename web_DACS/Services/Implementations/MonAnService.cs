using web_DACS.Models;
using web_DACS.Repositories.Interfaces;
using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class MonAnService : IMonAnService
    {
        private readonly IMonAnRepository _monAnRepo;

        public MonAnService(IMonAnRepository monAnRepo)
        {
            _monAnRepo = monAnRepo;
        }

        public async Task<ApiResponse> GetAllAsync(string? searchString = null)
        {
            var items = await _monAnRepo.GetAllAsync(searchString);
            return ApiResponse.Ok("Lấy danh sách món ăn thành công.", items);
        }

        public async Task<ApiResponse> GetByIdAsync(int id)
        {
            var item = await _monAnRepo.GetByIdAsync(id);
            if (item == null) return ApiResponse.Fail("Không tìm thấy món ăn.");
            return ApiResponse.Ok("Thành công.", item);
        }

        public async Task<ApiResponse> CreateAsync(string tenMon, string? moTa, decimal gia, string? loai, string? hinhAnh)
        {
            if (string.IsNullOrWhiteSpace(tenMon))
                return ApiResponse.Fail("Tên món ăn không được để trống.");
            if (gia <= 0)
                return ApiResponse.Fail("Giá món ăn phải lớn hơn 0.");

            var monAn = new MonAn
            {
                TenMon = tenMon,
                MoTa = moTa,
                Gia = gia,
                Loai = loai,
                HinhAnh = hinhAnh
            };

            await _monAnRepo.AddAsync(monAn);
            await _monAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Thêm món ăn thành công.", monAn);
        }

        public async Task<ApiResponse> UpdateAsync(int id, string tenMon, string? moTa, decimal gia, string? loai, string? hinhAnh)
        {
            if (string.IsNullOrWhiteSpace(tenMon))
                return ApiResponse.Fail("Tên món ăn không được để trống.");
            if (gia <= 0)
                return ApiResponse.Fail("Giá món ăn phải lớn hơn 0.");

            var item = await _monAnRepo.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Fail("Không tìm thấy món ăn cần cập nhật.");

            item.TenMon = tenMon;
            item.MoTa = moTa;
            item.Gia = gia;
            item.Loai = loai;
            item.HinhAnh = hinhAnh;

            _monAnRepo.Update(item);
            await _monAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Cập nhật món ăn thành công.", item);
        }

        public async Task<ApiResponse> DeleteAsync(int id)
        {
            var item = await _monAnRepo.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Fail("Không tìm thấy món ăn cần xóa.");

            await _monAnRepo.DeleteAsync(id);
            await _monAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Xóa món ăn thành công.");
        }

        public async Task<ApiResponse> SeedDataIfEmptyAsync()
        {
            if (await _monAnRepo.AnyAsync())
                return ApiResponse.Ok("Dữ liệu đã tồn tại, bỏ qua seed.");

            var danhSachMon = new List<MonAn>
            {
                new() { TenMon = "Tôm Hùm Nướng Phô Mai", Gia = 450000, Loai = "Food", HinhAnh = "tomphomai.jpg", MoTa = "Tôm hùm tươi sống nướng cùng lớp phô mai Mozzarella tan chảy." },
                new() { TenMon = "Cua Rang Me", Gia = 350000, Loai = "Food", HinhAnh = "cua.jpg", MoTa = "Cua thịt chắc ngọt quyện cùng sốt me chua cay đậm đà." },
                new() { TenMon = "Coca Cola", Gia = 15000, Loai = "Drink", HinhAnh = "coca.jpg", MoTa = "Nước giải khát có ga." }
            };

            await _monAnRepo.AddRangeAsync(danhSachMon);
            await _monAnRepo.SaveChangesAsync();
            return ApiResponse.Ok("Seed data đã được thêm.");
        }
    }
}
