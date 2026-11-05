using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Reports;

public class QuestPdfReportDocument : IDocument
{
    private readonly AttendanceReportViewModel _model;
    private readonly byte[]? _kiranaLogoBytes;
    private readonly byte[]? _sigLogoBytes;

    public QuestPdfReportDocument(AttendanceReportViewModel model)
    {
        _model = model;
        _kiranaLogoBytes = LoadImageBytes("logo-kirana.png");
        _sigLogoBytes = LoadImageBytes("logo-sig.png");
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (var section in _model.Sections)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36, Unit.Point); // Normal standard A4 document margins
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8f).FontColor("#000000"));

                page.Header().Element(c => ComposeHeader(c, section));
                page.Content().Element(c => ComposeContent(c, section));
            });
        }
    }

    private void ComposeHeader(IContainer container, EmployeeReportSection section)
    {
        container.Column(col =>
        {
            // Row 1: Top Logos across full width (Kirana at far left, SIG at far right)
            col.Item().Row(row =>
            {
                // Left Logo Kirana
                row.ConstantItem(120).AlignLeft().AlignMiddle().Element(c =>
                {
                    if (_kiranaLogoBytes != null)
                    {
                        c.Height(32).Image(_kiranaLogoBytes).FitArea();
                    }
                    else
                    {
                        c.Text("KIRANA").FontSize(13).Bold().FontColor("#02559C");
                    }
                });

                row.RelativeItem(); // Space between logos

                // Right Logo SIG
                row.ConstantItem(120).AlignRight().AlignMiddle().Element(c =>
                {
                    if (_sigLogoBytes != null)
                    {
                        c.Height(32).Image(_sigLogoBytes).FitArea();
                    }
                    else
                    {
                        c.Text("SIG").FontSize(15).Bold().FontColor("#000000");
                    }
                });
            });

            // Row 2: Title centered directly UNDERNEATH the logos (spans 100% full printable width, dead-center)
            col.Item().PaddingTop(4).AlignCenter().Text(text =>
            {
                text.AlignCenter();
                text.Line("ABSENSI ENGINEER ON SITE").FontSize(11.5f).Bold().FontColor("#000000");
                text.Line("MANAGE SERVICE SIG").FontSize(11f).Bold().FontColor("#000000");
            });

            // Row 3: Metadata (Nama, Periode, Lokasi)
            col.Item().PaddingTop(6).Table(metaTable =>
            {
                metaTable.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(48);
                    cols.ConstantColumn(12);
                    cols.RelativeColumn();
                });

                metaTable.Cell().PaddingVertical(0.6f).Text("Nama").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(":").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(section.Employee.FullName).FontSize(8f).Bold();

                metaTable.Cell().PaddingVertical(0.6f).Text("Periode").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(":").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(_model.PeriodText).FontSize(8f);

                metaTable.Cell().PaddingVertical(0.6f).Text("Lokasi").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(":").FontSize(8f);
                metaTable.Cell().PaddingVertical(0.6f).Text(_model.Location).FontSize(8f);
            });

            col.Item().PaddingBottom(4);
        });
    }

    private void ComposeContent(IContainer container, EmployeeReportSection section)
    {
        container.Column(col =>
        {
            // Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);   // No
                    columns.ConstantColumn(96);   // Tanggal (e.g. 15 Oktober 2026)
                    columns.ConstantColumn(52);   // Hari (e.g. Selasa)
                    columns.ConstantColumn(64);   // Waktu Masuk (e.g. 07:22)
                    columns.ConstantColumn(64);   // Waktu Keluar (e.g. 16:30)
                    columns.RelativeColumn();     // Keterangan
                });

                // Table Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("No");
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("Tanggal");
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("Hari");
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("Waktu Masuk");
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("Waktu Keluar");
                    header.Cell().Element(HeaderStyle).AlignCenter().AlignMiddle().Text("Keterangan");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.Border(0.6f).BorderColor("#000000").PaddingVertical(2.2f).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(7.5f).Bold().FontColor("#000000"));
                });

                // Table Rows
                foreach (var row in section.Rows)
                {
                    table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(row.No.ToString());
                    table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(row.FormattedDate);
                    table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(row.DayName);
                    table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(row.CheckInText);
                    table.Cell().Element(CellStyle).AlignCenter().AlignMiddle().Text(row.CheckOutText);
                    table.Cell().Element(CellStyle).AlignLeft().AlignMiddle().Text(row.Description);
                }

                static IContainer CellStyle(IContainer container) =>
                    container.Border(0.6f).BorderColor("#000000").PaddingVertical(1.6f).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(7f).FontColor("#000000"));
            });

            // Spacing before signatures directly under the table
            col.Item().PaddingTop(12);

            // Three Signature Blocks (Dibuat oleh, Mengetahui, Menyetujui) directly below table
            col.Item().Element(c => ComposeSignatures(c, section));
        });
    }

    private void ComposeSignatures(IContainer container, EmployeeReportSection section)
    {
        container.Row(row =>
        {
            // 1. Dibuat oleh (Engineer On Site)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(section.CreatedBy.Header).FontSize(8f);
                c.Item().AlignCenter().Text(section.CreatedBy.Company).FontSize(8.5f).Bold();
                c.Item().Height(34); // Space for manual physical signature
                c.Item().AlignCenter().Text(section.CreatedBy.Name).FontSize(8.5f).Bold();
                c.Item().AlignCenter().Text(section.CreatedBy.Title).FontSize(7.5f);
            });

            // 2. Mengetahui (Project Manager MS WiFi)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Header).FontSize(8f);
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Company).FontSize(8.5f).Bold();
                c.Item().Height(34); // Space for manual physical signature
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Name).FontSize(8.5f).Bold();
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Title).FontSize(7.5f);
            });

            // 3. Menyetujui (IT Site Infrastructure Officer)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.ApprovedBy.Header).FontSize(8f);
                c.Item().AlignCenter().Text(_model.ApprovedBy.Company).FontSize(8.5f).Bold();
                c.Item().Height(34); // Space for manual physical signature
                c.Item().AlignCenter().Text(_model.ApprovedBy.Name).FontSize(8.5f).Bold();
                c.Item().AlignCenter().Text(_model.ApprovedBy.Title).FontSize(7.5f);
            });
        });
    }

    private static byte[]? LoadImageBytes(string relativeFileName)
    {
        try
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", relativeFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", relativeFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "SiteAttendance", "wwwroot", "images", relativeFileName),
                Path.Combine("D:\\We-recap\\SiteAttendance\\wwwroot\\images", relativeFileName)
            };

            foreach (var p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    return File.ReadAllBytes(p);
                }
            }
        }
        catch
        {
            // fallback
        }
        return null;
    }
}
