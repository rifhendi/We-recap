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
                page.Margin(26, Unit.Point);
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
            // Logos & Title Row
            col.Item().Row(row =>
            {
                // Left: Logo KIRANA
                row.ConstantItem(120).AlignLeft().Element(c =>
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

                // Center: Title
                row.RelativeItem().AlignCenter().Column(titleCol =>
                {
                    titleCol.Item().AlignCenter().Text("ABSENSI ENGINEER ON SITE").FontSize(11.5f).Bold().FontColor("#000000");
                    titleCol.Item().AlignCenter().Text("MANAGE SERVICE SIG").FontSize(11f).Bold().FontColor("#000000");
                });

                // Right: Logo SIG
                row.ConstantItem(120).AlignRight().Element(c =>
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

            // Metadata: Nama, Periode, Lokasi
            col.Item().Row(metaRow =>
            {
                metaRow.ConstantItem(55).Column(c =>
                {
                    c.Item().Text("Nama").FontSize(8.5f);
                    c.Item().Text("Periode").FontSize(8.5f);
                    c.Item().Text("Lokasi").FontSize(8.5f);
                });

                metaRow.ConstantItem(12).Column(c =>
                {
                    c.Item().Text(":").FontSize(8.5f);
                    c.Item().Text(":").FontSize(8.5f);
                    c.Item().Text(":").FontSize(8.5f);
                });

                metaRow.RelativeItem().Column(c =>
                {
                    c.Item().Text(section.Employee.FullName).FontSize(8.5f).Bold();
                    c.Item().Text(_model.PeriodText).FontSize(8.5f);
                    c.Item().Text(_model.Location).FontSize(8.5f);
                });
            });

            col.Item().PaddingBottom(8);
        });
    }

    private void ComposeContent(IContainer container, EmployeeReportSection section)
    {
        container.Column(column =>
        {
            // Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);   // No
                    columns.ConstantColumn(110);  // Tanggal (e.g. 15 September 2026)
                    columns.ConstantColumn(56);   // Hari (e.g. Selasa)
                    columns.ConstantColumn(68);   // Waktu Masuk (e.g. 07:10)
                    columns.ConstantColumn(68);   // Waktu Keluar (e.g. 17:15)
                    columns.RelativeColumn();     // Keterangan
                });

                // Table Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("No");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Tanggal");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Hari");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Waktu Masuk");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Waktu Keluar");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Keterangan");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.Border(0.6f).BorderColor("#000000").PaddingVertical(2.5f).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(8f).Bold().FontColor("#000000"));
                });

                // Table Rows
                foreach (var row in section.Rows)
                {
                    table.Cell().Element(CellStyle).AlignCenter().Text(row.No.ToString());
                    table.Cell().Element(CellStyle).AlignCenter().Text(row.FormattedDate);
                    table.Cell().Element(CellStyle).AlignCenter().Text(row.DayName);
                    table.Cell().Element(CellStyle).AlignCenter().Text(row.CheckInText);
                    table.Cell().Element(CellStyle).AlignCenter().Text(row.CheckOutText);
                    table.Cell().Element(CellStyle).AlignLeft().Text(row.Description);
                }

                static IContainer CellStyle(IContainer container) =>
                    container.Border(0.6f).BorderColor("#000000").PaddingVertical(1.6f).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(7.5f).FontColor("#000000"));
            });

            // Spacing before signatures
            column.Item().PaddingTop(18);

            // Three Signature Blocks (Dibuat oleh, Mengetahui, Menyetujui)
            column.Item().Element(c => ComposeSignatures(c, section));
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
                c.Item().Height(38); // Space for manual signature
                c.Item().AlignCenter().Text(section.CreatedBy.Name).FontSize(8.5f).Bold();
                c.Item().AlignCenter().Text(section.CreatedBy.Title).FontSize(7.5f);
            });

            // 2. Mengetahui (Project Manager MS WiFi)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Header).FontSize(8f);
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Company).FontSize(8.5f).Bold();
                c.Item().Height(38); // Space for manual signature
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Name).FontSize(8.5f).Bold();
                c.Item().AlignCenter().Text(_model.AcknowledgedBy.Title).FontSize(7.5f);
            });

            // 3. Menyetujui (IT Site Infrastructure Officer)
            row.RelativeItem().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.ApprovedBy.Header).FontSize(8f);
                c.Item().AlignCenter().Text(_model.ApprovedBy.Company).FontSize(8.5f).Bold();
                c.Item().Height(38); // Space for manual signature
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
