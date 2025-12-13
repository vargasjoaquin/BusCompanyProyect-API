using Proyecto_EmpresaBus_API.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Proyecto_EmpresaBus_API.Helpers
{
    public static class PdfGenerator
    {
        public static byte[] GenerarTicketPdf(Viaje viaje, Usuario usuario, List<int> asientos, decimal total)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BUX APP").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Ticket de Viaje Electrónico").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(50).Text("🎫").FontSize(30);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Código de Viaje: #{viaje.ViajeID}").Bold();
                        col.Item().Text($"Fecha Emisión: {DateTime.Now:dd/MM/yyyy}");

                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("TITULAR DE LA RESERVA").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Nombre: {usuario.NombreCompleto}");
                        col.Item().Text($"Email: {usuario.Email}");

                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("DETALLES DEL SERVICIO").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Empresa: {viaje.Autobus?.Empresa?.NombreEmpresa ?? "Partner"}").Bold();
                        col.Item().Text($"Unidad: {viaje.Autobus?.NumeroBus} - {viaje.Autobus?.Modelo}");
                        col.Item().Text($"Plataforma: {viaje.Plataforma ?? "A confirmar"}");

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("ORIGEN").FontSize(8).FontColor(Colors.Grey.Darken2);
                                c.Item().Text(viaje.Ruta.Origen.NombreLocalidad).FontSize(14).Bold();
                                c.Item().Text(viaje.FechaSalida.ToString("HH:mm") + " hs").FontSize(12).SemiBold();
                                c.Item().Text(viaje.FechaSalida.ToString("dd/MM/yyyy"));
                            });

                            row.ConstantItem(20).AlignCenter().Text("➔").FontSize(16).FontColor(Colors.Grey.Medium);

                            row.RelativeItem().Column(c => {
                                c.Item().AlignRight().Text("DESTINO").FontSize(8).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignRight().Text(viaje.Ruta.Destino.NombreLocalidad).FontSize(14).Bold();
                                if (viaje.FechaLlegadaEstimada.HasValue)
                                {
                                    c.Item().AlignRight().Text(viaje.FechaLlegadaEstimada.Value.ToString("HH:mm") + " hs").FontSize(12).SemiBold();
                                    c.Item().AlignRight().Text(viaje.FechaLlegadaEstimada.Value.ToString("dd/MM/yyyy"));
                                }
                            });
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("Asientos").FontSize(10);
                                c.Item().Text(string.Join(", ", asientos)).FontSize(16).Bold();
                            });
                            row.RelativeItem().AlignRight().Column(c => {
                                c.Item().AlignRight().Text("Total Pagado").FontSize(10);
                                c.Item().AlignRight().Text($"${total:N2}").FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Gracias por viajar con nosotros. ");
                        x.Span("Bux App").Bold();
                    });
                });
            }).GeneratePdf();
        }
    }
}
