using ClosedXML.Excel;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Helpers
{
    public static class ExcelHelper
    {
        public static byte[] GenerateAttendanceReport(List<AttendanceRecord> records, string sessionTitle)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("كشف الحضور");

            // Header styling
            sheet.Cell(1, 1).Value = $"تقرير حضور - {sessionTitle}";
            sheet.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Column headers
            var headers = new[] { "م", "اسم الطالب", "الرقم الجامعي", "البريد الإلكتروني", "المادة", "الحالة", "وقت التسجيل" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(2, i + 1).Value = headers[i];
                sheet.Cell(2, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.DarkBlue)
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            // Data rows
            int row = 3;
            int seq = 1;
            foreach (var record in records)
            {
                sheet.Cell(row, 1).Value = seq++;
                sheet.Cell(row, 2).Value = record.User?.FullName ?? "-";
                sheet.Cell(row, 3).Value = record.User?.StudentId ?? "-";
                sheet.Cell(row, 4).Value = record.User?.Email ?? "-";
                sheet.Cell(row, 5).Value = record.Session?.Course?.Name ?? "-";
                sheet.Cell(row, 6).Value = record.Status == AttendanceStatus.OnTime ? "حاضر في الوقت" : "حاضر متأخر";
                sheet.Cell(row, 7).Value = record.ScannedAt.ToString("yyyy-MM-dd HH:mm");

                if (record.Status == AttendanceStatus.Late)
                    sheet.Cell(row, 6).Style.Font.SetFontColor(XLColor.OrangeRed);
                else
                    sheet.Cell(row, 6).Style.Font.SetFontColor(XLColor.DarkGreen);

                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}