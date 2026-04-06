using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Security;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tài khoản người dùng trên SQL Server
    /// </summary>
    public class CustomerAccountRepository : BaseRepository, IUserAccountRepository
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public CustomerAccountRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Xác thực tài khoản khách hàng
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // Giả sử bảng Customers có các cột Email (tên đăng nhập) và Password
                var sql = @"SELECT  CustomerID as UserId, 
                                    Email as UserName, 
                                    CustomerName as DisplayName, 
                                    Email, 
                                    '' as Photo, 
                                    'customer' as RoleNames
                            FROM Customers 
                            WHERE Email = @Email AND Password = @Password";
                var data = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { Email = userName, Password = CryptHelper.HashMD5(password) });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangePassword(string userName, string password)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = "UPDATE Customers SET Password = @Password WHERE Email = @Email";
                var result = await connection.ExecuteAsync(sql, new { Email = userName, Password = CryptHelper.HashMD5(password) }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
    }
}
