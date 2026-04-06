using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.DataLayers.SQLServer;
using SV22T1020796.Models.Security;

namespace SV22T1020796.BusinessLayers
{
    /// <summary>
    /// Các loại tài khoản
    /// </summary>
    public enum AccountTypes
    {
        Employee,
        Customer
    }

    /// <summary>
    /// Cung cấp các chức năng liên quan đến quản lý tài khoản người dùng
    /// </summary>
    public static class UserAccountService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        static UserAccountService()
        {
            string connectionString = Configuration.ConnectionString;
            employeeAccountDB = new EmployeeAccountRepository(connectionString);
            customerAccountDB = new CustomerAccountRepository(connectionString);
        }

        /// <summary>
        /// Xác thực tài khoản người dùng
        /// </summary>
        /// <param name="accountType"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<UserAccount?> Authorize(AccountTypes accountType, string userName, string password)
        {
            if (accountType == AccountTypes.Employee)
                return await employeeAccountDB.Authorize(userName, password);
            else
                return await customerAccountDB.Authorize(userName, password);
        }

        /// <summary>
        /// Thay đổi mật khẩu
        /// </summary>
        /// <param name="accountType"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<bool> ChangePassword(AccountTypes accountType, string userName, string password)
        {
            if (accountType == AccountTypes.Employee)
                return await employeeAccountDB.ChangePassword(userName, password);
            else
                return await customerAccountDB.ChangePassword(userName, password);
        }
    }
}
