using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SiteAttendance.Models;
using SiteAttendance.ViewModels;

namespace SiteAttendance.Reports;

public class QuestPdfReportDocument : IDocument
{
    private readonly AttendanceReportViewModel _model;

    public QuestPdfReportDocument(AttendanceReportViewModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28, Unit.Point);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PT SEMEN GRESIK (PERSERO) TBK.").FontSize(11).Bold().FontColor("#0F2042");
                    c.Item().Text("MANAGE SERVICE SIG - SITE REMBANG").FontSize(10).SemiBold().FontColor("#1A365D");
                    c.Item().Text("Jln. Raya Rembang - Blora KM 17, Rembang, Jawa Tengah").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(120).AlignRight().Column(c =>
                {
                    c.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(4).AlignCenter().Text("FORM ABSENSI").FontSize(9).Bold().FontColor("#0F2042");
                    c.Item().PaddingTop(2).Text($"Periode: {_model.StartDate:dd/MM/yy} - {_model.EndDate:dd/MM/yy}").FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(6).PaddingBottom(6).LineHorizontal(1.5f).LineColor("#0F2042");

            col.Item().AlignCenter().Column(c =>
            {
                c.Item().Text("ABSENSI ENGINEER ON SITE").FontSize(13).Bold().FontColor("#0F2042").LetterSpacing(0.5f);
                c.Item().Text("MANAGE SERVICE SIG").FontSize(10).SemiBold().FontColor("#334155");
                c.Item().Text($"Lokasi: {_model.Location}  |  Periode: {_model.PeriodText}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            });

            col.Item().PaddingBottom(8);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            foreach (var section in _model.Sections)
            {
                column.Item().Element(c => ComposeEngineerSection(c, section));
                column.Item().PaddingBottom(12);
            }

            // Summary Box
            column.Item().Element(ComposeSummaryTable);

            // Three Signature Blocks (Dibuat Oleh, Mengetahui, Menyetujui)
            column.Item().PaddingTop(16).Element(ComposeSignatureBlocks);
        });
    }

    private void ComposeEngineerSection(IContainer container, EmployeeReportSection section)
    {
        container.Column(col =>
        {
            // Engineer Info Bar
            col.Item().Background("#0F2042").Padding(5).Row(row =>
            {
                row.RelativeItem().Text($"Engineer: {section.Employee.FullName} ({section.Employee.EmployeeCode}) - {section.Employee.Position}").FontSize(9).Bold().FontColor(Colors.White);
                row.ConstantItem(150).AlignRight().Text($"Total Hadir: {section.HadirCount} Hari | {section.TotalHours:0.##} Jam").FontSize(8.5f).FontColor(Colors.White);
            });

            // Attendance Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);  // No
                    columns.ConstantColumn(68);  // Tanggal
                    columns.ConstantColumn(52);  // Hari
                    columns.ConstantColumn(54);  // Jam Masuk
                    columns.ConstantColumn(54);  // Jam Pulang
                    columns.ConstantColumn(50);  // Jam Kerja
                    columns.ConstantColumn(50);  // Status
                    columns.RelativeColumn();    // Keterangan
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("No");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Tanggal");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Hari");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Jam Masuk");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Jam Pulang");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Jam (H)");
                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Status");
                    header.Cell().Element(HeaderStyle).Text("Keterangan");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.Background("#E2E8F0").Border(0.5f).BorderColor("#CBD5E1").PaddingVertical(3).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(8).Bold().FontColor("#1E293B"));
                });

                // Rows
                foreach (var row in section.Rows)
                {
                    var isEven = row.No % 2 == 0;
                    var bg = isEven ? "#F8FAFC" : "#FFFFFF";

                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.No.ToString());
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.Date.ToString("dd/MM/yyyy"));
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.DayName);
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.CheckInText);
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.CheckOutText);
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.WorkHours > 0 ? row.WorkHours.ToString("0.##") : "-");
                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(row.Status.ToString()).SemiBold().FontColor(GetStatusColor(row.Status));
                    table.Cell().Element(c => CellStyle(c, bg)).Text(row.Description);
                }

                static IContainer CellStyle(IContainer container, string background) =>
                    container.Background(background).Border(0.5f).BorderColor("#E2E8F0").PaddingVertical(2.5f).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(7.5f));
            });
        });
    }

    private void ComposeSummaryTable(IContainer container)
    {
        container.Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("REKAPITULASI KEHADIRAN:").FontSize(8).Bold().FontColor("#0F2042");
                c.Item().Text($"Total Hadir: {_model.TotalHadir} hari  |  Izin: {_model.TotalIzin}  |  Sakit: {_model.TotalSakit}  |  Cuti: {_model.TotalCuti}  |  Libur: {_model.TotalLibur}").FontSize(8).FontColor("#334155");
            });

            row.ConstantItem(140).AlignRight().Column(c =>
            {
                c.Item().Text($"Total Jam Kerja: {_model.TotalWorkHours:0.##} Jam").FontSize(8.5f).Bold().FontColor("#0F2042");
                c.Item().Text($"Dicetak pada: {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm} WIB").FontSize(7).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeSignatureBlocks(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).Text("TANDA TANGAN & PENGESAHAN:").FontSize(8).Bold().FontColor("#0F2042");

            col.Item().Row(row =>
            {
                // 1. DIBUAT OLEH
                row.RelativeItem().Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(c =>
                {
                    c.Item().AlignCenter().Text(_model.CreatedBy.Header).FontSize(8).Bold().FontColor("#0F2042");
                    c.Item().Height(42); // Ruang tanda tangan manual
                    c.Item().AlignCenter().Text(_model.CreatedBy.Name).FontSize(8.5f).Bold().Underline();
                    c.Item().AlignCenter().Text(_model.CreatedBy.Title).FontSize(7).FontColor("#475569");
                });

                row.ConstantItem(8); // Spacer

                // 2. MENGETAHUI
                row.RelativeItem().Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(c =>
                {
                    c.Item().AlignCenter().Text(_model.AcknowledgedBy.Header).FontSize(8).Bold().FontColor("#0F2042");
                    c.Item().Height(42); // Ruang tanda tangan manual
                    c.Item().AlignCenter().Text(_model.AcknowledgedBy.Name).FontSize(8.5f).Bold().Underline();
                    c.Item().AlignCenter().Text(_model.AcknowledgedBy.Title).FontSize(7).FontColor("#475569");
                });

                row.ConstantItem(8); // Spacer

                // 3. MENYETUJUI
                row.RelativeItem().Border(0.5f).BorderColor("#CBD5E1").Padding(6).Column(c =>
                {
                    c.Item().AlignCenter().Text(_model.ApprovedBy.Header).FontSize(8).Bold().FontColor("#0F2042");
                    c.Item().Height(42); // Ruang tanda tangan manual
                    c.Item().AlignCenter().Text(_model.ApprovedBy.Name).FontSize(8.5f).Bold().Underline();
                    c.Item().AlignCenter().Text(_model.ApprovedBy.Title).FontSize(7).FontColor("#475569");
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Dokumen ini digenerate secara otomatis oleh Site Attendance Management System (Manage Service SIG Site Rembang)").FontSize(6.5f).FontColor(Colors.Grey.Medium);
            row.ConstantItem(100).AlignRight().Text(x =>
            {
                x.Span("Halaman ");
                x.CurrentPageNumber();
                x.Span(" dari ");
                x.TotalPages();
            });
        });
    }

    private static string GetStatusColor(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Hadir => "#047857", // Green
        AttendanceStatus.Izin => "#B45309",  // Amber
        AttendanceStatus.Sakit => "#B91C1C", // Red
        AttendanceStatus.Cuti => "#4338CA",  // Indigo
        AttendanceStatus.Libur => "#64748B", // Slate
        _ => "#000000"
    };
}

