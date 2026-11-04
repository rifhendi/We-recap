using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SiteAttendance.Reports;
using SiteAttendance.ViewModels;
using System.Globalization;
using System.Text;

namespace SiteAttendance.Services;

public interface IExportService
{
    byte[] ExportToPdf(AttendanceReportViewModel model);
    byte[] ExportToExcel(AttendanceReportViewModel model);
    byte[] ExportToCsv(AttendanceReportViewModel model);
    string GetFileName(AttendanceReportViewModel model, string extension);
}

public class ExportService : IExportService
{
    public ExportService()
    {
        // Konfigurasi lisensi QuestPDF Community
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToPdf(AttendanceReportViewModel model)
    {
        var document = new QuestPdfReportDocument(model);
        return document.GeneratePdf();
    }

    public byte[] ExportToExcel(AttendanceReportViewModel model)
    {
        using var workbook = new XLWorkbook();

        foreach (var section in model.Sections)
        {
            var sheetName = SanitizeSheetName(section.Employee.FullName);
            var ws = workbook.Worksheets.Add(sheetName);

            // Title & Info Header
            ws.Cell(1, 1).Value = "ABSENSI ENGINEER ON SITE - MANAGE SERVICE SIG";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0F2042");

            ws.Cell(2, 1).Value = $"Lokasi: {model.Location}  |  Periode: {model.PeriodText}";
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

            ws.Cell(3, 1).Value = $"Engineer: {section.Employee.FullName} ({section.Employee.EmployeeCode}) - {section.Employee.Position}";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 11;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

            // Table Headers
            int startRow = 5;
            string[] headers = { "No", "Tanggal", "Hari", "Jam Masuk", "Jam Pulang", "Total Jam Kerja", "Status", "Keterangan" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(startRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F2042");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Table Rows
            int currentRow = startRow + 1;
            foreach (var r in section.Rows)
            {
                ws.Cell(currentRow, 1).Value = r.No;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = r.Date.ToString("yyyy-MM-dd");
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 3).Value = r.DayName;
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 4).Value = r.CheckInText;
                ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 5).Value = r.CheckOutText;
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 6).Value = r.WorkHours;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(currentRow, 7).Value = r.Status.ToString();
                ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 7).Style.Font.Bold = true;

                ws.Cell(currentRow, 8).Value = r.Description;

                if (currentRow % 2 == 0)
                {
                    ws.Range(currentRow, 1, currentRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                currentRow++;
            }

            // Border table
            var tableRange = ws.Range(startRow, 1, currentRow - 1, 8);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

            // Rekapitulasi Baris
            currentRow += 1;
            ws.Cell(currentRow, 1).Value = $"Rekapitulasi: Hadir={section.HadirCount}, Izin={section.IzinCount}, Sakit={section.SakitCount}, Cuti={section.CutiCount}, Libur={section.LiburCount} | Total Jam Kerja={section.TotalHours:0.##} Jam";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#0F2042");
            ws.Range(currentRow, 1, currentRow, 8).Merge();

            // Signature Section
            currentRow += 3;
            // Dibuat Oleh
            ws.Cell(currentRow, 2).Value = model.CreatedBy.Header;
            ws.Cell(currentRow, 2).Style.Font.Bold = true;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Mengetahui
            ws.Cell(currentRow, 5).Value = model.AcknowledgedBy.Header;
            ws.Cell(currentRow, 5).Style.Font.Bold = true;
            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Menyetujui
            ws.Cell(currentRow, 7).Value = model.ApprovedBy.Header;
            ws.Cell(currentRow, 7).Style.Font.Bold = true;
            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow += 4; // Space for signature

            ws.Cell(currentRow, 2).Value = model.CreatedBy.Name;
            ws.Cell(currentRow, 2).Style.Font.Bold = true;
            ws.Cell(currentRow, 2).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 5).Value = model.AcknowledgedBy.Name;
            ws.Cell(currentRow, 5).Style.Font.Bold = true;
            ws.Cell(currentRow, 5).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 7).Value = model.ApprovedBy.Name;
            ws.Cell(currentRow, 7).Style.Font.Bold = true;
            ws.Cell(currentRow, 7).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow += 1;
            ws.Cell(currentRow, 2).Value = model.CreatedBy.Title;
            ws.Cell(currentRow, 2).Style.Font.FontSize = 8;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 5).Value = model.AcknowledgedBy.Title;
            ws.Cell(currentRow, 5).Style.Font.FontSize = 8;
            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 7).Value = model.ApprovedBy.Title.Replace("\n", " - ");
            ws.Cell(currentRow, 7).Style.Font.FontSize = 8;
            ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToCsv(AttendanceReportViewModel model)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        // Header info
        writer.WriteLine($"# ABSENSI ENGINEER ON SITE - MANAGE SERVICE SIG");
        writer.WriteLine($"# Lokasi: {model.Location}");
        writer.WriteLine($"# Periode: {model.PeriodText}");
        writer.WriteLine();

        csv.WriteField("EmployeeCode");
        csv.WriteField("FullName");
        csv.WriteField("Position");
        csv.WriteField("No");
        csv.WriteField("Tanggal");
        csv.WriteField("Hari");
        csv.WriteField("JamMasuk");
        csv.WriteField("JamPulang");
        csv.WriteField("TotalJamKerja");
        csv.WriteField("Status");
        csv.WriteField("Keterangan");
        csv.NextRecord();

        foreach (var section in model.Sections)
        {
            foreach (var r in section.Rows)
            {
                csv.WriteField(section.Employee.EmployeeCode);
                csv.WriteField(section.Employee.FullName);
                csv.WriteField(section.Employee.Position);
                csv.WriteField(r.No);
                csv.WriteField(r.Date.ToString("yyyy-MM-dd"));
                csv.WriteField(r.DayName);
                csv.WriteField(r.CheckInText);
                csv.WriteField(r.CheckOutText);
                csv.WriteField(r.WorkHours);
                csv.WriteField(r.Status.ToString());
                csv.WriteField(r.Description);
                csv.NextRecord();
            }
        }

        writer.Flush();
        return ms.ToArray();
    }

    public string GetFileName(AttendanceReportViewModel model, string extension)
    {
        var sanitizedExt = extension.TrimStart('.');
        string namePart = model.SelectedEmployee != null
            ? model.SelectedEmployee.FullName.Replace(" ", "_")
            : "Semua_Engineer";

        string periodPart = $"{model.StartDate:ddMMM}-{model.EndDate:ddMMMyyyy}";
        return $"Absensi_{namePart}_{periodPart}.{sanitizedExt}";
    }

    private static string SanitizeSheetName(string name)
    {
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var clean = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        return clean.Length > 28 ? clean.Substring(0, 28) : clean;
    }
}
