namespace SV22T1020796.Models.Sales
{
    /// <summary>
    /// Mở rộng các phương thức cho enum OrderStatusEnum
    /// </summary>
    public static class OrderStatusExtensions
    {
        /// <summary>
        /// Lấy chuỗi mô tả cho từng trạng thái của đơn hàng
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string GetDescription(this OrderStatusEnum status)
        {
            return status switch
            {
                OrderStatusEnum.Rejected => "Đơn hàng bị từ chối",
                OrderStatusEnum.Cancelled => "Đơn hàng đã bị hủy",
                OrderStatusEnum.New => "Đơn hàng vừa tạo",
                OrderStatusEnum.Accepted => "Đơn hàng đã được duyệt",
                OrderStatusEnum.Shipping => "Đơn hàng đang được vận chuyển",
                OrderStatusEnum.Completed => "Đơn hàng đã hoàn tất",
                _ => "Không xác định"
            };
        }
        /// <summary>
        /// Lấy lớp CSS màu sắc cho trạng thái
        /// </summary>
        public static string GetColorClass(this OrderStatusEnum status)
        {
            return status switch
            {
                OrderStatusEnum.Rejected => "bg-danger text-white",
                OrderStatusEnum.Cancelled => "bg-secondary text-white",
                OrderStatusEnum.New => "bg-warning text-dark",
                OrderStatusEnum.Accepted => "bg-primary text-white",
                OrderStatusEnum.Shipping => "bg-info text-dark",
                OrderStatusEnum.Completed => "bg-success text-white",
                _ => "bg-dark text-white"
            };
        }
    }
}
