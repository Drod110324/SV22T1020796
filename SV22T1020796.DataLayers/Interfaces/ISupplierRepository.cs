using SV22T1020796.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020796.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu đặc thù cho nhà cung cấp
    /// </summary>
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra xem email có bị trùng hay không
        /// </summary>
        /// <param name="email"></param>
        /// <param name="supplierID"></param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int supplierID);
    }
}
