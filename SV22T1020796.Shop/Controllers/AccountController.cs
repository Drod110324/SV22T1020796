using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020796.BusinessLayers;
using SV22T1020796.Models.Security;
using SV22T1020796.Models.Partner;
using System.Security.Claims;

namespace SV22T1020796.Shop.Controllers
{
    /// <summary>
    /// Controller quản lý tài khoản người dùng của trang Shop
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị trang đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            ViewBag.UserName = userName;
            ViewBag.Password = password;

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }

            var userAccount = await UserAccountService.Authorize(AccountTypes.Customer, userName, password);
            if (userAccount != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userAccount.UserId.ToString()),
                    new Claim(ClaimTypes.Name, userAccount.DisplayName),
                    new Claim(ClaimTypes.Email, userAccount.UserName),
                    new Claim(ClaimTypes.Role, "customer")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            return View();
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Hiển thị trang đăng ký tài khoản
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng ký tài khoản
        /// </summary>
        /// <param name="customerName"></param>
        /// <param name="phone"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Register(string customerName, string phone, string email, string password, string confirmPassword)
        {
            // Xóa tất cả các lỗi mặc định (nếu có) để sử dụng thông báo tiếng Việt thủ công
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(customerName))
                ModelState.AddModelError("customerName", "Họ và tên không được để trống.");
            
            if (string.IsNullOrWhiteSpace(phone))
                ModelState.AddModelError("phone", "Số điện thoại không được để trống.");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9,10}$"))
                ModelState.AddModelError("phone", "Số điện thoại không đúng định dạng (VD: 0912345678).");

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("email", "Email không được để trống.");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                ModelState.AddModelError("email", "Email không đúng định dạng (VD: name@domain.com).");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Mật khẩu không được để trống.");
            else if (password.Length < 6)
                ModelState.AddModelError("password", "Mật khẩu phải có ít nhất 6 ký tự.");
            
            if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
            {
                return View();
            }

            // Kiểm tra email trùng
            if (!await PartnerDataService.ValidatelCustomerEmailAsync(email))
            {
                ModelState.AddModelError("email", "Email này đã được sử dụng bởi một tài khoản khác.");
                return View();
            }

            // Lấy danh sách tỉnh thành để lấy 1 giá trị mặc định hợp lệ (tránh lỗi khóa ngoại)
            var provinces = await DictionaryDataService.ListProvincesAsync();
            string defaultProvince = provinces.FirstOrDefault()?.ProvinceName ?? "Khác";

            var customer = new Customer
            {
                CustomerName = customerName,
                ContactName = customerName,
                Phone = phone,
                Email = email,
                Password = password,
                Province = defaultProvince,
                Address = "",
                IsLocked = false
            };

            int id = await PartnerDataService.AddCustomerAsync(customer);
            if (id > 0)
            {
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.";
            return View();
        }

        /// <summary>
        /// Hiển thị thông tin cá nhân
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "customer")]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login");

            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(id));
            return View(customer);
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Authorize(Roles = "customer")]
        [HttpPost]
        public async Task<IActionResult> Profile(Customer data)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName) || string.IsNullOrWhiteSpace(data.Phone))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
                return View(data);
            }

            var existing = await PartnerDataService.GetCustomerAsync(data.CustomerID);
            if (existing != null)
            {
                existing.CustomerName = data.CustomerName;
                existing.Phone = data.Phone;
                existing.Province = data.Province;
                existing.Address = data.Address;
                
                // Giữ nguyên email và password nếu không đổi ở đây (thông thường password đổi ở action khác)
                bool result = await PartnerDataService.UpdateCustomerAsync(existing);
                if (result)
                {
                    ViewBag.SuccessMessage = "Cập nhật thông tin thành công.";
                    return View(existing);
                }
            }

            ViewBag.ErrorMessage = "Cập nhật thông tin không thành công.";
            return View(data);
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="confirmPassword"></param>
        /// <returns></returns>
        [Authorize(Roles = "customer")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ (Authorize)
            var auth = await UserAccountService.Authorize(AccountTypes.Customer, email, oldPassword);
            if (auth == null)
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không chính xác.";
                return RedirectToAction("Profile");
            }

            bool result = await UserAccountService.ChangePassword(AccountTypes.Customer, email, newPassword);
            if (result)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Đổi mật khẩu không thành công.";
            }

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Hiển thị trang từ chối truy cập
        /// </summary>
        /// <returns></returns>
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
