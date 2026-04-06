
namespace SV22T1020796.BusinessLayers
{
    /// <summary>
    /// Khởi tạo và lưu trữ cấu hình chung của BusinessLayer
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Chuỗi kết nối đến CSDL
        /// </summary>
        public static string ConnectionString { get; private set; } = string.Empty;

        /// <summary>
        /// Khởi tạo cấu hình cho BusinessLayer
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
