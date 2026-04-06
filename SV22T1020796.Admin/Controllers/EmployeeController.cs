using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.HR;
using SV22T1020796.BusinessLayers;

namespace SV22T1020796.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý nhân viên
    /// </summary>
    public class EmployeeController : Controller
    {
        private static readonly int PAGESIZE = 10;

        /// <summary>
        /// Hiển thị danh sách nhân viên (với tìm kiếm + phân trang)
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("EmployeeSearch");
            if (input == null || Request.Query.ContainsKey("page") || Request.Query.ContainsKey("searchValue"))
            {
                input = new PaginationSearchInput()
                {
                    Page = page,
                    PageSize = PAGESIZE,
                    SearchValue = searchValue
                };
                ApplicationContext.SetSessionData("EmployeeSearch", input);
            }

            var result = await HRDataService.ListEmployeesAsync(input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị form bổ sung nhân viên mới (GET)
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa thông tin nhân viên (GET)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu nhân viên (thêm mới hoặc cập nhật) (POST)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="roles"></param>
        /// <param name="uploadPhoto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Save(Employee data, string[] roles, IFormFile? uploadPhoto)
        {
            try
            {
                data.RoleNames = string.Join(",", roles);
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                    TempData["SuccessMessage"] = "Bổ sung nhân viên thành công!";
                }
                else
                {
                    // Nếu là cập nhật và mật khẩu để trống thì lấy lại mật khẩu cũ
                    if (string.IsNullOrWhiteSpace(data.Password))
                    {
                        var oldData = await HRDataService.GetEmployeeAsync(data.EmployeeID);
                        data.Password = oldData?.Password ?? "";
                    }
                    
                    await HRDataService.UpdateEmployeeAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa nhân viên (GET)
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        public async Task<IActionResult> Delete(int id)
        {
            if (await HRDataService.IsUsedEmployeeAsync(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa nhân viên này vì có dữ liệu liên quan (đơn hàng, ...)";
                return RedirectToAction("Index");
            }

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
            {
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        /// <summary>
        /// Xử lý xóa nhân viên (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, Employee data)
        {
            try
            {
                bool result = await HRDataService.DeleteEmployeeAsync(id);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhân viên. Nhân viên có thể đang có dữ liệu liên quan.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Xóa nhân viên thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị trang thay đổi quyền hạn cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        public async Task<IActionResult> ChangeRole(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            ViewBag.EmployeeID = id;
            ViewBag.FullName = employee.FullName;
            ViewBag.Photo = employee.Photo;

            // Danh sách quyền hạn giả (Dạng danh sách các đối tượng ẩn danh)
            ViewBag.AllRoles = new[]
            {
                new { Name = "Quản trị", Desc = "Toàn quyền hệ thống", Selected = true },
                new { Name = "Bán hàng", Desc = "Quản lý đơn hàng", Selected = false },
                new { Name = "Thủ kho", Desc = "Quản lý kho hàng", Selected = true },
                new { Name = "Giao hàng", Desc = "Cập nhật vận chuyển", Selected = false }
            };

            return View();
        }

        /// <summary>
        /// Xử lý lưu thay đổi quyền hạn nhân viên
        /// </summary>
        [HttpPost]
        public IActionResult ChangeRole(int EmployeeID, string[] roleNames)
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            ViewBag.EmployeeID = id;
            ViewBag.FullName = employee.FullName;

            return View();
        }

        /// <summary>
        /// Xử lý lưu mật khẩu mới cho nhân viên
        /// </summary>
        [HttpPost]
        public IActionResult ChangePassword(int id, string newPassword, string confirmPassword)
        {
            return RedirectToAction("Index");
        }
    }
}
