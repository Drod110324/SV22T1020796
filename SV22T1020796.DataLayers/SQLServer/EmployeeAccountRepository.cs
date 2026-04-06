using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Security;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tài khoản nhân viên (Admin) trên SQL Server
    /// </summary>
    public class EmployeeAccountRepository : BaseRepository, IUserAccountRepository
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public EmployeeAccountRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Xác thực tài khoản nhân viên
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT  EmployeeID as UserId, 
                                    Email as UserName, 
                                    FullName as DisplayName, 
                                    Email, 
                                    Photo, 
                                    RoleNames
                            FROM Employees 
                            WHERE Email = @Email AND Password = @Password AND IsWorking = 1";
                var data = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { Email = userName, Password = CryptHelper.HashMD5(password) });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangePassword(string userName, string password)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = "UPDATE Employees SET Password = @Password WHERE Email = @Email";
                var result = await connection.ExecuteAsync(sql, new { Email = userName, Password = CryptHelper.HashMD5(password) }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
    }
}
