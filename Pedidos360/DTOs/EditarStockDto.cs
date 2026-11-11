using System.ComponentModel.DataAnnotations;

public class EditarStockDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int StockActual { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int NuevoStock { get; set; }
}