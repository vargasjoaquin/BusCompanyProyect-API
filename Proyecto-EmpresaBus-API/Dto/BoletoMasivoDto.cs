namespace Proyecto_EmpresaBus_API.Dto
{
    public class BoletoMasivoDto
    {
        public int ViajeID { get; set; }
        public int UsuarioID { get; set; }
        public List<int> Asientos { get; set; }
    }
}
