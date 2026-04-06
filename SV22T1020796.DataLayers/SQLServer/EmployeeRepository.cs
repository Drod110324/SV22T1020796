using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.HR;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employee) trên SQL Server
    /// </summary>
    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public EmployeeRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một nhân viên mới
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames)
                            VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Password, @Photo, @IsWorking, @RoleNames);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<int>(sql, new {
                    data.FullName, data.BirthDate, data.Address, data.Phone, data.Email,
                    Password = CryptHelper.HashMD5(data.Password), data.Photo, data.IsWorking, data.RoleNames
                });
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Xóa nhân viên dựa trên mã
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                var result = await connection.ExecuteAsync(sql, new { EmployeeID = id }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin một nhân viên dựa trên mã
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                var data = await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có đang tham gia xử lý các đơn hàng hay không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID) THEN 1 
                                ELSE 0 
                            END";
                var result = await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên có phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                SearchValue = input.SearchValue
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Employees 
                            WHERE (FullName LIKE @SearchValue) OR (Email LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                            ORDER BY FullName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Employees 
                            WHERE (FullName LIKE @SearchValue) OR (Email LIKE @SearchValue) OR (Phone LIKE @SearchValue);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Employee>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees 
                            SET FullName = @FullName, 
                                BirthDate = @BirthDate, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email, 
                                Password = @Password,
                                Photo = @Photo, 
                                IsWorking = @IsWorking,
                                RoleNames = @RoleNames
                            WHERE EmployeeID = @EmployeeID";
                var result = await connection.ExecuteAsync(sql, new {
                    data.FullName, data.BirthDate, data.Address, data.Phone, data.Email,
                    Password = (data.Password?.Length == 32) ? data.Password : CryptHelper.HashMD5(data.Password),
                    data.Photo, data.IsWorking, data.RoleNames, data.EmployeeID
                }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Kiểm tra email có bị trùng hay không
        /// </summary>
        /// <param name="email"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT COUNT(*) FROM Employees 
                            WHERE Email = @Email AND EmployeeID <> @EmployeeID";
                var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
                await connection.CloseAsync();
                return count == 0;
            }
        }
    }
}
