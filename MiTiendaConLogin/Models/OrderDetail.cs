namespace MiTiendaConLogin.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Guardamos el precio al momento de la compra

        // Clave foránea para la Orden
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Clave foránea para el Producto
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}