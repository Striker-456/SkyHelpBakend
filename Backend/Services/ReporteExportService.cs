using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkyHelp.DTOs.Reportes;
using SkyHelp.Services.Interfaces;
using System.Text;

namespace SkyHelp.Services
{
    public class ReporteExportService : IReporteExportService
    {
        public byte[] ExportarCsv(ReporteGeneradoDto reporte)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", reporte.Columnas.Select(EscaparCsv)));
            foreach (var fila in reporte.Filas)
            {
                var valores = reporte.Columnas.Select(c => EscaparCsv(fila.TryGetValue(c, out var v) ? v?.ToString() ?? "" : ""));
                sb.AppendLine(string.Join(",", valores));
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public byte[] ExportarExcel(ReporteGeneradoDto reporte)
        {
            using var workbook = new XLWorkbook();
            var hoja = workbook.Worksheets.Add(LimitarNombreHoja(reporte.Titulo));

            for (var i = 0; i < reporte.Columnas.Count; i++)
                hoja.Cell(1, i + 1).Value = reporte.Columnas[i];
            hoja.Row(1).Style.Font.Bold = true;

            for (var f = 0; f < reporte.Filas.Count; f++)
            {
                for (var c = 0; c < reporte.Columnas.Count; c++)
                {
                    var valor = reporte.Filas[f].TryGetValue(reporte.Columnas[c], out var v) ? v : null;
                    hoja.Cell(f + 2, c + 1).Value = XLCellValue.FromObject(valor ?? "");
                }
            }
            hoja.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportarPdf(ReporteGeneradoDto reporte)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text(reporte.Titulo).FontSize(16).Bold();
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in reporte.Columnas)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var columna in reporte.Columnas)
                                header.Cell().Text(columna).Bold().FontSize(9);
                        });

                        foreach (var fila in reporte.Filas)
                        {
                            foreach (var columna in reporte.Columnas)
                            {
                                var valor = fila.TryGetValue(columna, out var v) ? v?.ToString() ?? "" : "";
                                table.Cell().Text(valor).FontSize(8);
                            }
                        }
                    });
                    page.Footer().AlignRight().Text($"Generado el {reporte.FechaGeneracion:dd/MM/yyyy HH:mm}").FontSize(8);
                });
            });

            return documento.GeneratePdf();
        }

        private static string EscaparCsv(string valor)
        {
            if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            return valor;
        }

        private static string LimitarNombreHoja(string nombre)
        {
            var limpio = new string(nombre.Where(c => !"[]:*?/\\".Contains(c)).ToArray());
            return limpio.Length > 31 ? limpio[..31] : (limpio.Length == 0 ? "Reporte" : limpio);
        }
    }
}
