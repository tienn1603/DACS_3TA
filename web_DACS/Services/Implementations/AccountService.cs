using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using web_DACS.Models;
using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<ApiResponse> RegisterAsync(string username, string email, string password, string fullName, string? phoneNumber, string? address)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ApiResponse.Fail("Username không được để trống.");
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponse.Fail("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(password))
                return ApiResponse.Fail("Password không được để trống.");

            if (await _userManager.FindByNameAsync(username) != null)
                return ApiResponse.Fail("Username đã tồn tại.");
            if (await _userManager.FindByEmailAsync(email) != null)
                return ApiResponse.Fail("Email đã tồn tại.");

            const string defaultRole = "User";
            if (!await _roleManager.RoleExistsAsync(defaultRole))
                await _roleManager.CreateAsync(new IdentityRole(defaultRole));

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Address = address
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return ApiResponse.Fail($"Đăng ký thất bại: {errors}");
            }

            await _userManager.AddToRoleAsync(user, defaultRole);

            return ApiResponse.Ok("Đăng ký thành công.", new
            {
                user.Id,
                user.UserName,
                user.Email,
                role = defaultRole
            });
        }

        public async Task<ApiResponse> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return ApiResponse.Fail("Username và password không được để trống.");

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return ApiResponse.Fail("Username hoặc password không đúng.");

            if (!await _userManager.CheckPasswordAsync(user, password))
                return ApiResponse.Fail("Username hoặc password không đúng.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";
            var token = GenerateJwtToken(user, role);

            return ApiResponse.Ok("Đăng nhập thành công.", new
            {
                token,
                role,
                user = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FullName
                }
            });
        }

        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
