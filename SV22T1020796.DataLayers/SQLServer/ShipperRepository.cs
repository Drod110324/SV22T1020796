using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Partner;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shipper) trên SQL Server
    /// </summary>
    public class ShipperRepository : BaseRepository, IGenericRepository<Shipper>
    {
        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public ShipperRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một người giao hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần bổ sung</param>
        /// <returns>Mã của người giao hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            int id = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Shippers(ShipperName, Phone)
                            VALUES(@ShipperName, @Phone);
                            SELECT @@IDENTITY;";
                id = await connection.ExecuteScalarAsync<int>(sql, data);
                await connection.CloseAsync();
            }
            return id;
        }

        /// <summary>
        /// Xóa một người giao hàng dựa vào mã id
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                result = await connection.ExecuteAsync(sql, new { ShipperID = id }) > 0;
                await connection.CloseAsync();
            }
            return result;
        }

        /// <summary>
        /// Lấy thông tin của một người giao hàng dựa vào mã id
        /// </summary>
        /// <param name="id">Mã người giao hàng cần lấy</param>
        /// <returns>Thông tin người giao hàng hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            Shipper? data = null;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
                data = await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
                await connection.CloseAsync();
            }
            return data;
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng có dữ liệu liên quan (ví dụ: trong bảng đơn hàng) hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng cần kiểm tra</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Orders WHERE ShipperID = @ShipperID) THEN 1 
                                ELSE 0 
                            END";
                result = await connection.ExecuteScalarAsync<bool>(sql, new { ShipperID = id });
                await connection.CloseAsync();
            }
            return result;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách người giao hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Tham số tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang chứa danh sách người giao hàng</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Shippers 
                            WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                            ORDER BY ShipperName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Shippers 
                            WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Shipper>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            bool result = false;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Shippers 
                            SET ShipperName = @ShipperName, 
                                Phone = @Phone 
                            WHERE ShipperID = @ShipperID";
                result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
            }
            return result;
        }
    }
}
