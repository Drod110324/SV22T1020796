using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.Models.DataDictionary;
using System.Data;

namespace SV22T1020796.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tỉnh thành (Province) trên SQL Server
    /// </summary>
    public class ProvinceRepository : BaseRepository, IDataDictionaryRepository<Province>
    {
        /// <summary>
        /// Khởi tạo Repository
        /// </summary>
        /// <param name="connectionString"></param>
        public ProvinceRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Lấy danh sách tất cả các tỉnh thành
        /// </summary>
        /// <returns></returns>
        public async Task<List<Province>> ListAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM Provinces";
                var data = (await connection.QueryAsync<Province>(sql)).ToList();
                await connection.CloseAsync();
                return data;
            }
        }
    }
}
