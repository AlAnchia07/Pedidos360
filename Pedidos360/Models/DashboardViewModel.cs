namespace Pedidos360.Models
{
    public class DashboardViewModel
    {
        public int TotalProductos { get; set; }
        public int TotalClientes { get; set; }
        public int TotalPedidos { get; set; }
        public int StockTotal { get; set; }
        public List<ProductoBajoStock> BajoStock { get; set; } = new();
    }

    public class ProductoBajoStock
    {
        public string ImagenUrl { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int Stock { get; set; }
        public bool Activo { get; set; }
    }
}