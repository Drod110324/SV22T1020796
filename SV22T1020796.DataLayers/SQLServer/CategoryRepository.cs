using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Catalog;
using SV22T1020796.Models.Common;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng (Category) trên SQL Server
    /// </summary>
    public class CategoryRepository : BaseRepository, IGenericRepository<Category>
    {
        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public CategoryRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một loại hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng</param>
        /// <returns>Mã loại hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Category data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Categories(CategoryName, Description)
                            VALUES(@CategoryName, @Description);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Xóa loại hàng dựa vào mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";
                var result = await connection.ExecuteAsync(sql, new { CategoryID = id }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin một loại hàng dựa vào mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Dữ liệu loại hàng hoặc null</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";
                var data = await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có đang được sử dụng bởi các mặt hàng hay không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu đã có mặt hàng thuộc loại này</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID = @CategoryID) THEN 1 
                                ELSE 0 
                            END";
                var result = await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách loại hàng có phân trang
        /// </summary>
        /// <param name="input">Tham số tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang chứa danh sách loại hàng</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Categories 
                            WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)
                            ORDER BY CategoryName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Categories 
                            WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Category>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Categories 
                            SET CategoryName = @CategoryName, 
                                Description = @Description
                            WHERE CategoryID = @CategoryID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
    }
}