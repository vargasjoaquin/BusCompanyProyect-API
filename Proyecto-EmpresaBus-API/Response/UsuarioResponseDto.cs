namespace Proyecto_EmpresaBus_API.Response
{
    public class UsuarioResponseDto
    {
        public int UsuarioID { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string DNI { get; set; }
        public string Rol { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public int? Edad { get; set; }
        public string? Sexo { get; set; }

        public string? Ciudad { get; set; }
        public string? Provincia { get; set; }
        public int? LocalidadID { get; set; 
        public int? ProvinciaID { get; set; }
    }
}
