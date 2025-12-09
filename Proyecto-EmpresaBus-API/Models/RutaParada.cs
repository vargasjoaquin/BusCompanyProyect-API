namespace Proyecto_EmpresaBus_API.Models
{
    public class RutaParada
    {
        public int RutaID { get; set; }
        public Ruta Ruta { get; set; }

        public int ParadaID { get; set; }
        public Parada Parada { get; set; }

        public int Orden { get; set; }
    }
}
