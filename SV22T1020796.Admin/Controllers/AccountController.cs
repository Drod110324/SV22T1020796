using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020796.BusinessLayers;
using SV22T1020796.Models.HR;
using SV22T1020796.Admin;
using System.Text.RegularExpressions;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý tài khoản người dùng
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị trang đăng nhập
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập tài khoản và mật khẩu!");
                return View();
            }

            var userAccount = await UserAccountService.Authorize(AccountTypes.Employee, username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Sai tài khoản hoặc mật khẩu!");
                return View();
            }

            // Chuẩn bị thông tin để ghi lên "giấy chứng nhận"
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',').ToList()
            };

            // Tạo giấy chứng nhận (ClaimsPrincipal)
            var principal = userData.CreatePrincipal();

            // Cấp giấy chứng nhận này cho người dùng (đăng nhập)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Hiển thị trang từ chối truy cập
        /// </summary>
        /// <returns></returns>
        public IActionResult AccessDenied()
        {
            return View();
        }



        /// <summary>
        /// Hiển thị trang thay đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="confirmPassword"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không trùng khớp!");
                return View();
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            var userAccount = await UserAccountService.Authorize(AccountTypes.Employee, userData.UserName, oldPassword);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Mật khẩu hiện tại không đúng!");
                return View();
            }

            await UserAccountService.ChangePassword(AccountTypes.Employee, userData.UserName, newPassword);
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}
