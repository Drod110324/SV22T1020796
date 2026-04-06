using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.Catalog;
using SV22T1020796.Models.Common;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng (Product) trên SQL Server
    /// </summary>
    public class ProductRepository : BaseRepository, IProductRepository
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public ProductRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Bổ sung một mặt hàng mới
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<int> AddAsync(Product data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                            VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Bổ sung một thuộc tính cho mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                            VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<long>(sql, data);
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Bổ sung một ảnh cho mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                            VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                            SELECT @@IDENTITY;";
                var id = await connection.ExecuteScalarAsync<long>(sql, data);
                await connection.CloseAsync();
                return id;
            }
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                            DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                            DELETE FROM Products WHERE ProductID = @ProductID;";
                var result = await connection.ExecuteAsync(sql, new { ProductID = productID }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Xóa thuộc tính của mặt hàng
        /// </summary>
        /// <param name="attributeID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
                var result = await connection.ExecuteAsync(sql, new { AttributeID = attributeID }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Xóa ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID"></param>
        /// <returns></returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
                var result = await connection.ExecuteAsync(sql, new { PhotoID = photoID }) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Lấy thông tin mặt hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<Product?> GetAsync(int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Products WHERE ProductID = @ProductID";
                var data = await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính dựa trên mã
        /// </summary>
        /// <param name="attributeID"></param>
        /// <returns></returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
                var data = await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Lấy thông tin một ảnh dựa trên mã
        /// </summary>
        /// <param name="photoID"></param>
        /// <returns></returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
                var data = await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Kiểm tra mặt hàng có đang được sử dụng hay không
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT CASE 
                                WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID) THEN 1 
                                ELSE 0 
                            END";
                var result = await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng có phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                SearchValue = input.SearchValue
            };

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"SELECT * FROM Products 
                            WHERE (ProductName LIKE @SearchValue OR @SearchValue = N'')
                                AND (CategoryID = @CategoryID OR @CategoryID = 0)
                                AND (SupplierID = @SupplierID OR @SupplierID = 0)
                                AND (Price >= @MinPrice OR @MinPrice = 0)
                                AND (Price <= @MaxPrice OR @MaxPrice = 0)
                            ORDER BY ProductName
                            OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

                            SELECT COUNT(*) FROM Products 
                            WHERE (ProductName LIKE @SearchValue OR @SearchValue = N'')
                                AND (CategoryID = @CategoryID OR @CategoryID = 0)
                                AND (SupplierID = @SupplierID OR @SupplierID = 0)
                                AND (Price >= @MinPrice OR @MinPrice = 0)
                                AND (Price <= @MaxPrice OR @MaxPrice = 0);";

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Page = input.Page,
                    PageSize = input.PageSize,
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice
                };

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.DataItems = (await multi.ReadAsync<Product>()).ToList();
                    result.RowCount = await multi.ReadFirstAsync<int>();
                }
                await connection.CloseAsync();
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                var data = (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                var data = (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
                await connection.CloseAsync();
                return data;
            }
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Products 
                            SET ProductName = @ProductName, 
                                ProductDescription = @ProductDescription, 
                                SupplierID = @SupplierID, 
                                CategoryID = @CategoryID, 
                                Unit = @Unit, 
                                Price = @Price, 
                                Photo = @Photo, 
                                IsSelling = @IsSelling
                            WHERE ProductID = @ProductID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Cập nhật thuộc tính
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductAttributes 
                            SET ProductID = @ProductID, 
                                AttributeName = @AttributeName, 
                                AttributeValue = @AttributeValue, 
                                DisplayOrder = @DisplayOrder
                            WHERE AttributeID = @AttributeID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }

        /// <summary>
        /// Cập nhật ảnh
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductPhotos 
                            SET ProductID = @ProductID, 
                                Photo = @Photo, 
                                Description = @Description, 
                                DisplayOrder = @DisplayOrder, 
                                IsHidden = @IsHidden
                            WHERE PhotoID = @PhotoID";
                var result = await connection.ExecuteAsync(sql, data) > 0;
                await connection.CloseAsync();
                return result;
            }
        }
    }
}
