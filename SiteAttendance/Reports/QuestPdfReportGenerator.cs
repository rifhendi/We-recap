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
                page.Margin(28, Unit.Point);
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
            // Logos & Title Table - Perfectly centered between left and right logos across full width
            col.Item().Table(headerTable =>
            {
                headerTable.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(120); // Left: Logo Kirana
                    cols.RelativeColumn();    // Center: Title (Perfect horizontal center on page)
                    cols.ConstantColumn(120); // Right: Logo SIG
                });

                // Col 1: Left Logo Kirana
                headerTable.Cell().AlignLeft().AlignMiddle().Element(c =>
                {
                    if (_kiranaLogoBytes != null)
                    {
                        c.Height(36).Image(_kiranaLogoBytes).FitArea();
                    }
                    else
                    {
                        c.Text("KIRANA").FontSize(13).Bold().FontColor("#02559C");
                    }
                });

                // Col 2: Center Title
                headerTable.Cell().AlignCenter().AlignMiddle().Column(titleCol =>
                {
                    titleCol.Item().AlignCenter().Text("ABSENSI ENGINEER ON SITE").FontSize(12f).Bold().FontColor("#000000");
                    titleCol.Item().AlignCenter().Text("MANAGE SERVICE SIG").FontSize(11.5f).Bold().FontColor("#000000");
                });

                // Col 3: Right Logo SIG
                headerTable.Cell().AlignRight().AlignMiddle().Element(c =>
                {
                    if (_sigLogoBytes != null)
                    {
                        c.Height(36).Image(_sigLogoBytes).FitArea();
                    }
                    else
                    {
                        c.Text("SIG").FontSize(15).Bold().FontColor("#000000");
                    }
                });
            });

            // Spacing
            col.Item().PaddingTop(12);

            // Metadata: Nama, Periode, Lokasi (Formatted as cleanly aligned 3-column table)
            col.Item().Table(metaTable =>
            {
                metaTable.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(46);
                    cols.ConstantColumn(12);
                    cols.RelativeColumn();
                });

                metaTable.Cell().PaddingVertical(1).Text("Nama").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(":").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(section.Employee.FullName).FontSize(8.5f).Bold();

                metaTable.Cell().PaddingVertical(1).Text("Periode").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(":").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(_model.PeriodText).FontSize(8.5f);

                metaTable.Cell().PaddingVertical(1).Text("Lokasi").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(":").FontSize(8.5f);
                metaTable.Cell().PaddingVertical(1).Text(_model.Location).FontSize(8.5f);
            });

            col.Item().PaddingBottom(8);
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
                    columns.ConstantColumn(26);   // No
                    columns.ConstantColumn(105);  // Tanggal (e.g. 15 September 2026)
                    columns.ConstantColumn(56);   // Hari (e.g. Selasa)
                    columns.ConstantColumn(72);   // Waktu Masuk (e.g. 08:00)
                    columns.ConstantColumn(72);   // Waktu Keluar (e.g. 17:00)
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
                        container.Border(0.6f).BorderColor("#000000").PaddingVertical(3.5f).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(8f).Bold().FontColor("#000000"));
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
                    container.Border(0.6f).BorderColor("#000000").PaddingVertical(2.8f).PaddingHorizontal(4).DefaultTextStyle(x => x.FontSize(7.5f).FontColor("#000000"));
            });

            // Spacing before signatures directly under the table
            col.Item().PaddingTop(20);

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
                c.Item().AlignCenter().Text(section.CreatedBy.Header).FontSize(8.5f);
                c.Item().AlignCenter().Text(section.CreatedBy.Company).FontSize(9f).Bold();
                c.Item().Height(46); // Space for manual physical signature
                c.Item().AlignCenter().Text(section.CreatedBy.Name).FontSize(9f).Bold();
                c.Item().AlignCenter().Text(section.CreatedBy.Title).FontSize(8f);
            });

            // 2. Mengetahui (Project Manager MS WiFi)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Header).FontSize(8.5f);
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Company).FontSize(9f).Bold();
                c.Item().Height(46); // Space for manual physical signature
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Name).FontSize(9f).Bold();
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Title).FontSize(8f);
            });

            // 3. Menyetujui (IT Site Infrastructure Officer)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.ApprovedBy.Header).FontSize(8.5f);
                c.Item().AlignCenter().Text(_model.ApprovedBy.Company).FontSize(9f).Bold();
                c.Item().Height(46); // Space for manual physical signature
                c.Item().AlignCenter().Text(_model.ApprovedBy.Name).FontSize(9f).Bold();
                c.Item().AlignCenter().Text(_model.ApprovedBy.Title).FontSize(8f);
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
