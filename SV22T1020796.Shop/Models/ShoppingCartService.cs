using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SV22T1020796.Models.Sales;

namespace SV22T1020796.Shop.Models
{
    public static class ShoppingCartService
    {
        private static IHttpContextAccessor? _httpContextAccessor;

        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private const string CART_SESSION_KEY = "ShoppingCart";

        public static List<OrderDetailViewInfo> GetCartItems()
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            if (session == null) return new List<OrderDetailViewInfo>();

            var json = session.GetString(CART_SESSION_KEY);
            return json == null ? new List<OrderDetailViewInfo>() : JsonConvert.DeserializeObject<List<OrderDetailViewInfo>>(json) ?? new List<OrderDetailViewInfo>();
        }

        public static void SaveCartItems(List<OrderDetailViewInfo> items)
        {
            var session = _httpContextAccessor?.HttpContext?.Session;
            if (session == null) return;

            var json = JsonConvert.SerializeObject(items);
            session.SetString(CART_SESSION_KEY, json);
        }

        public static void AddToCart(OrderDetailViewInfo item)
        {
            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(i => i.ProductID == item.ProductID);
            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                cart.Add(item);
            }
            SaveCartItems(cart);
        }

        public static void RemoveFromCart(int productID)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(i => i.ProductID == productID);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }
        }

        public static void ClearCart()
        {
            SaveCartItems(new List<OrderDetailViewInfo>());
        }
    }
}
