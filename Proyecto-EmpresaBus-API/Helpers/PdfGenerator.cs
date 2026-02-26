using Proyecto_EmpresaBus_API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proyecto_EmpresaBus_API.Helpers
{
    public static class PdfGenerator
    {
        /// <summary>
        /// Genera el archivo PDF del ticket de viaje.
        /// El documento incluye dos secciones (talones):
        /// uno para el chofer/transportista y otro para el pasajero,
        /// con información detallada del viaje, usuario, asientos
        /// y total abonado.
        /// </summary>
        /// <param name="viaje">Información del viaje.</param>
        /// <param name="usuario">Datos del pasajero.</param>
        /// <param name="asientos">Lista de asientos reservados.</param>
        /// <param name="total">Importe total abonado.</param>
        /// <returns>Archivo PDF en formato byte.</returns>
        public static byte[] GenerarTicketPdf(Viaje viaje, Usuario usuario, List<int> asientos, decimal total)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(9).FontColor(Colors.Black));

                    page.Content().Column(col =>
                    {
                        col.Item().Element(c => DiseñarTalon(c, viaje, usuario, asientos, total, "Talón para el Chofer/Transportista"));

                        col.Item().PaddingVertical(15).Row(row =>
                        {
                            row.RelativeItem().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            row.ConstantItem(100).AlignCenter().Text(" CORTE AQUÍ (✂) ").FontSize(8).FontColor(Colors.Grey.Darken2);

                            row.RelativeItem().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        });

                        col.Item().Element(c => DiseñarTalon(c, viaje, usuario, asientos, total, "Talón para el Pasajero"));
                    });
                });
            }).GeneratePdf();
        }

        /// <summary>
        /// Construye el diseño visual de cada talón dentro del ticket.
        /// Se encarga de estructurar la información del viaje,
        /// empresa, pasajero, plataforma, horarios, importe
        /// y datos comerciales en el formato definido.
        /// </summary>
        /// <param name="container">Contenedor gráfico de QuestPDF.</param>
        /// <param name="viaje">Información del viaje.</param>
        /// <param name="usuario">Datos del pasajero.</param>
        /// <param name="asientos">Lista de asientos reservados.</param>
        /// <param name="total">Importe total abonado.</param>
        /// <param name="tipoTalon">Descripción del tipo de talón.</param>
        private static void DiseñarTalon(IContainer container, Viaje viaje, Usuario usuario, List<int> asientos, decimal total, string tipoTalon)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Text(viaje.Autobus?.Empresa?.NombreEmpresa ?? "BUX TRANSPORTES S.A.").Bold().FontSize(12);
                    row.RelativeItem().AlignRight().Text($"--- {tipoTalon} ---").Italic().FontSize(9);
                });

                col.Item().BorderBottom(1).BorderColor(Colors.Black);

                col.Item().PaddingVertical(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Boleto Nro: ").Bold(); t.Span($"#{viaje.ViajeID:D8}"); });
                        c.Item().Text(t => { t.Span("Origen: ").Bold(); t.Span(viaje.Ruta.Origen.NombreLocalidad); });
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Butaca: ").Bold(); t.Span(string.Join(", ", asientos)).FontSize(11).Bold(); });
                        c.Item().Text(t => { t.Span("Plataforma: ").Bold(); t.Span(viaje.Plataforma ?? "Conf."); });
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Salida: ").Bold(); t.Span(viaje.FechaSalida.ToString("dddd - dd/MM/yyyy HH:mm")); });
                        c.Item().Text(t => { t.Span("Destino: ").Bold(); t.Span(viaje.Ruta.Destino.NombreLocalidad); });
                    });
                });

                col.Item().BorderBottom(1).BorderColor(Colors.Black);

                col.Item().PaddingTop(5).Text("Ud. viaja por: " + (viaje.Autobus?.Empresa?.NombreEmpresa ?? "Bux App")).FontSize(14).Bold();

                col.Item().PaddingVertical(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Se anuncia a: {viaje.Ruta.Destino.NombreLocalidad.ToUpper()}");
                        c.Item().Text($"Arribo Estimado: {viaje.FechaLlegadaEstimada?.ToString("dd/MM/yyyy HH:mm") ?? "-"}");
                    });
                });

                col.Item().PaddingVertical(2).Text(t =>
                {
                    t.Span("Pasajero: ").Bold();
                    t.Span($"{usuario.NombreCompleto.ToUpper()} - DNI: {usuario.DNI ?? "N/A"}");
                });

                col.Item().PaddingVertical(5).Row(row =>
                {
                    row.RelativeItem().Text($"Vendedor: BUX-WEB - {DateTime.Now:dd/MM/yyyy HH:mm}");

                    row.ConstantItem(200).AlignRight().Column(c => {
                        c.Item().AlignRight().Text($"Importe $ {total:N2}").FontSize(14).Bold();
                        c.Item().AlignRight().Text("Ref: WEB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()).FontSize(7);
                    });
                });

                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                col.Item().AlignCenter().Text("Conserve este talón para su control").FontSize(7).Italic();

                col.Item().PaddingTop(5).Column(c =>
                {
                    c.Item().Text(viaje.Autobus?.Empresa?.NombreEmpresa ?? "BUX TRANSPORTES S.A.").Bold();
                    c.Item().Text("Domicilio Comercial: Av. Siempre Viva 742 - C.A.B.A").FontSize(7);
                    c.Item().Text("Atención al Cliente: 0800-BUX-VIAJE").FontSize(7);
                });

                col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Colors.Black);
            });
        }
    }
}