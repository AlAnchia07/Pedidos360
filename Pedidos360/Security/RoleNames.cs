namespace Pedidos360.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Ventas = "Ventas";
    public const string Operaciones = "Operaciones";
    public const string AllRoles = Admin + "," + Ventas + "," + Operaciones;
    public const string AdminAndSales = Admin + "," + Ventas;
    public const string AdminOrOperaciones = Admin + "," + Operaciones;

    public static readonly string[] All = [Admin, Ventas, Operaciones];
}
