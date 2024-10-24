using System;
using System.Collections.Generic;

namespace urbanx.ViewModels
{
    public class CarritoViewModel
    {
        public int ProductoId { get; set; }
        public string? Nombre { get; set; }        // Nombre del producto
        public string? ImageURL { get; set; }      // URL de la imagen del producto
        public string? Talla { get; set; }         // Talla seleccionada
        public int Cantidad { get; set; }          // Cantidad total del grupo
        public decimal Precio { get; set; }        // Precio unitario
        public decimal Subtotal => Precio * Cantidad;  // Cálculo del subtotal
        public List<int> ItemIds { get; set; } = new List<int>();  // Lista de IDs de los items agrupados
        public string ItemIdsString => string.Join(",", ItemIds);   // IDs como string para pasar a la vista
    }

    public class CarritoEditViewModel
    {
        public string ItemIds { get; set; } = string.Empty;  // IDs de los items a editar
        public int ProductoId { get; set; }
        public string? Nombre { get; set; }
        public string? ImageURL { get; set; }
        public string? Talla { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public string? UserID { get; set; }        // Mantener el UserID para la actualización
    }

    public class CarritoResumenViewModel
    {
        public string? Producto { get; set; }
        public int CantProdu { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Precio * CantProdu;  // Añadido para calcular el subtotal automáticamente
    }
}