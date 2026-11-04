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
            ws.Cell(1, 1).Value = "ABSENSI ENGINEER ON SITE";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, 6).Merge();

            ws.Cell(2, 1).Value = "MANAGE SERVICE SIG";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 12;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(2, 1, 2, 6).Merge();

            // Metadata
            ws.Cell(4, 1).Value = "Nama";
            ws.Cell(4, 2).Value = $": {section.Employee.FullName}";
            ws.Cell(4, 2).Style.Font.Bold = true;

            ws.Cell(5, 1).Value = "Periode";
            ws.Cell(5, 2).Value = $": {model.PeriodText}";

            ws.Cell(6, 1).Value = "Lokasi";
            ws.Cell(6, 2).Value = $": {model.Location}";

            // Table Headers
            int startRow = 8;
            string[] headers = { "No", "Tanggal", "Hari", "Waktu Masuk", "Waktu Keluar", "Keterangan" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(startRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Table Rows
            int currentRow = startRow + 1;
            foreach (var r in section.Rows)
            {
                ws.Cell(currentRow, 1).Value = r.No;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = r.FormattedDate;
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 3).Value = r.DayName;
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 4).Value = r.CheckInText;
                ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 5).Value = r.CheckOutText;
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 6).Value = r.Description;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                currentRow++;
            }

            // Border table
            var tableRange = ws.Range(startRow, 1, currentRow - 1, 6);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Signature Section
            currentRow += 2;
            // Dibuat Oleh
            ws.Cell(currentRow, 2).Value = section.CreatedBy.Header;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow + 1, 2).Value = section.CreatedBy.Company;
            ws.Cell(currentRow + 1, 2).Style.Font.Bold = true;
            ws.Cell(currentRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Mengetahui
            ws.Cell(currentRow, 4).Value = model.AcknowledgedBy.Header;
            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow + 1, 4).Value = model.AcknowledgedBy.Company;
            ws.Cell(currentRow + 1, 4).Style.Font.Bold = true;
            ws.Cell(currentRow + 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Menyetujui
            ws.Cell(currentRow, 6).Value = model.ApprovedBy.Header;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow + 1, 6).Value = model.ApprovedBy.Company;
            ws.Cell(currentRow + 1, 6).Style.Font.Bold = true;
            ws.Cell(currentRow + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow += 5; // Ruang tanda tangan manual

            ws.Cell(currentRow, 2).Value = section.CreatedBy.Name;
            ws.Cell(currentRow, 2).Style.Font.Bold = true;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 4).Value = model.AcknowledgedBy.Name;
            ws.Cell(currentRow, 4).Style.Font.Bold = true;
            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 6).Value = model.ApprovedBy.Name;
            ws.Cell(currentRow, 6).Style.Font.Bold = true;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            currentRow += 1;
            ws.Cell(currentRow, 2).Value = section.CreatedBy.Title;
            ws.Cell(currentRow, 2).Style.Font.FontSize = 9;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 4).Value = model.AcknowledgedBy.Title;
            ws.Cell(currentRow, 4).Style.Font.FontSize = 9;
            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 6).Value = model.ApprovedBy.Title;
            ws.Cell(currentRow, 6).Style.Font.FontSize = 9;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 20;
            ws.Column(3).Width = 14;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 42;
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
        csv.WriteField("WaktuMasuk");
        csv.WriteField("WaktuKeluar");
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
