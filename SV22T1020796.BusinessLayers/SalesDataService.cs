using SV22T1020796.DataLayers.Interfaces;
using SV22T1020796.DataLayers.SQLServer;
using SV22T1020796.Models.Common;
using SV22T1020796.Models.Sales;

namespace SV22T1020796.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public static async Task<int> AddOrderAsync(Order data)
        {
            data.Status = OrderStatusEnum.New;
            data.OrderTime = DateTime.Now;

            return await orderDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật
            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            //TODO: Kiểm tra trạng thái đơn hàng trước khi xóa
            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var orderView = await orderDB.GetAsync(orderID);
            if (orderView == null || orderView.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.UpdateStatusAsync(orderID, OrderStatusEnum.Accepted, DateTime.Now, null, employeeID);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var orderView = await orderDB.GetAsync(orderID);
            if (orderView == null || orderView.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.UpdateStatusAsync(orderID, OrderStatusEnum.Rejected, null, DateTime.Now, employeeID);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var orderView = await orderDB.GetAsync(orderID);
            if (orderView == null) return false;
            
            if (orderView.Status != OrderStatusEnum.New && orderView.Status != OrderStatusEnum.Accepted)
                return false;

            return await orderDB.UpdateStatusAsync(orderID, OrderStatusEnum.Cancelled, null, DateTime.Now, null);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var orderView = await orderDB.GetAsync(orderID);
            if (orderView == null || orderView.Status != OrderStatusEnum.Accepted)
                return false;

            var order = new Order
            {
                OrderID = orderView.OrderID,
                CustomerID = orderView.CustomerID,
                OrderTime = orderView.OrderTime,
                DeliveryProvince = orderView.DeliveryProvince,
                DeliveryAddress = orderView.DeliveryAddress,
                EmployeeID = orderView.EmployeeID,
                AcceptTime = orderView.AcceptTime,
                ShipperID = shipperID,
                ShippedTime = DateTime.Now,
                FinishedTime = orderView.FinishedTime,
                Status = OrderStatusEnum.Shipping
            };

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var orderView = await orderDB.GetAsync(orderID);
            if (orderView == null || orderView.Status != OrderStatusEnum.Shipping)
                return false;

            var order = new Order
            {
                OrderID = orderView.OrderID,
                CustomerID = orderView.CustomerID,
                OrderTime = orderView.OrderTime,
                DeliveryProvince = orderView.DeliveryProvince,
                DeliveryAddress = orderView.DeliveryAddress,
                EmployeeID = orderView.EmployeeID,
                AcceptTime = orderView.AcceptTime,
                ShipperID = orderView.ShipperID,
                ShippedTime = orderView.ShippedTime,
                FinishedTime = DateTime.Now,
                Status = OrderStatusEnum.Completed
            };

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi thêm mặt hàng
            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            //TODO: Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật mặt hàng
            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            //TODO: Kiểm tra trạng thái đơn hàng trước khi xóa mặt hàng
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion
    }
}