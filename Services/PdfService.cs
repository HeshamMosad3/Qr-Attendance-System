using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Services
{
    public class PdfService : IPdfService
    {
        // ألوان المشروع
        private static readonly Color NavyBlue =
            new DeviceRgb(30, 58, 95);
        private static readonly Color LightBlue =
            new DeviceRgb(45, 106, 159);
        private static readonly Color SuccessGreen =
            new DeviceRgb(40, 167, 69);
        private static readonly Color WarningOrange =
            new DeviceRgb(253, 126, 20);
        private static readonly Color LightGray =
            new DeviceRgb(248, 249, 250);
        private static readonly Color BorderGray =
            new DeviceRgb(222, 226, 230);
        private static readonly Color TextDark =
            new DeviceRgb(33, 37, 41);
        private static readonly Color TextMuted =
            new DeviceRgb(108, 117, 125);

        public byte[] GenerateAttendanceReport(
            Session session,
            List<AttendanceRecord> records,
            string doctorName)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);

            doc.SetMargins(40, 40, 40, 40);

            // Arabic font — استخدم Helvetica كـ fallback
            var font = PdfFontFactory.CreateFont(
                StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(
                StandardFonts.HELVETICA_BOLD);

            // ===== Header =====
            AddHeader(doc, fontBold, font, session, doctorName);

            // ===== Summary Cards =====
            AddSummaryCards(doc, fontBold, font, records);

            // ===== Attendance Table =====
            AddAttendanceTable(doc, fontBold, font, records);

            // ===== Footer =====
            AddFooter(doc, font);

            doc.Close();
            return ms.ToArray();
        }

        public byte[] GenerateStudentReport(
            AppUser student,
            List<AttendanceRecord> records)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);

            doc.SetMargins(40, 40, 40, 40);

            var font = PdfFontFactory.CreateFont(
                StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(
                StandardFonts.HELVETICA_BOLD);

            // Header
            AddStudentHeader(doc, fontBold, font, student);

            // Stats per course
            AddStudentCourseStats(doc, fontBold, font,
                student, records);

            // Full records table
            AddStudentRecordsTable(doc, fontBold, font, records);

            AddFooter(doc, font);
            doc.Close();
            return ms.ToArray();
        }

        // ============================================================
        // Session Report Helpers
        // ============================================================

        private void AddHeader(Document doc, PdfFont bold,
            PdfFont regular, Session session, string doctorName)
        {
            // Header Background
            var headerTable = new Table(1)
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            var headerCell = new Cell()
                .SetBackgroundColor(NavyBlue)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(20)
                .SetTextAlignment(TextAlignment.CENTER);

            // University Name
            headerCell.Add(new Paragraph(
                "Egyptian E-Learning University")
                .SetFont(bold)
                .SetFontSize(16)
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginBottom(4));

            headerCell.Add(new Paragraph(
                "Faculty of Business Administration")
                .SetFont(regular)
                .SetFontSize(12)
                .SetFontColor(new DeviceRgb(200, 220, 240))
                .SetMarginBottom(12));

            // Report Title
            headerCell.Add(new Paragraph(
                "Attendance Report")
                .SetFont(bold)
                .SetFontSize(20)
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginBottom(4));

            headerCell.Add(new Paragraph(
                session.Title)
                .SetFont(regular)
                .SetFontSize(14)
                .SetFontColor(new DeviceRgb(200, 220, 240)));

            headerTable.AddCell(headerCell);
            doc.Add(headerTable);

            // Session Info Row
            var infoTable = new Table(
                UnitValue.CreatePercentArray(
                    new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            AddInfoCard(infoTable, bold, regular,
                "Course", session.Course?.Name ?? "-");
            AddInfoCard(infoTable, bold, regular,
                "Type", session.Type.ToString());
            AddInfoCard(infoTable, bold, regular,
                "Date",
                session.SessionDate.ToString("dd/MM/yyyy"));
            AddInfoCard(infoTable, bold, regular,
                "Doctor", doctorName);

            doc.Add(infoTable);

            // Divider
            doc.Add(new LineSeparator(
                new iText.Kernel.Pdf.Canvas.Draw
                    .SolidLine(1f))
                .SetStrokeColor(LightBlue)
                .SetMarginBottom(16));
        }

        private void AddInfoCard(Table table, PdfFont bold,
            PdfFont regular, string label, string value)
        {
            var cell = new Cell()
                .SetBackgroundColor(LightGray)
                .SetBorder(new SolidBorder(BorderGray, 0.5f))
                .SetPadding(10)
                .SetTextAlignment(TextAlignment.CENTER);

            cell.Add(new Paragraph(label)
                .SetFont(regular)
                .SetFontSize(9)
                .SetFontColor(TextMuted)
                .SetMarginBottom(2));

            cell.Add(new Paragraph(value)
                .SetFont(bold)
                .SetFontSize(11)
                .SetFontColor(NavyBlue));

            table.AddCell(cell);
        }

        private void AddSummaryCards(Document doc,
            PdfFont bold, PdfFont regular,
            List<AttendanceRecord> records)
        {
            int total = records.Count;
            int onTime = records.Count(
                r => r.Status == AttendanceStatus.OnTime);
            int late = records.Count(
                r => r.Status == AttendanceStatus.Late);
            double onTimePct = total > 0
                ? Math.Round((double)onTime / total * 100, 1)
                : 0;

            doc.Add(new Paragraph("Attendance Summary")
                .SetFont(bold)
                .SetFontSize(13)
                .SetFontColor(NavyBlue)
                .SetMarginBottom(8));

            var statsTable = new Table(
                UnitValue.CreatePercentArray(
                    new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            AddStatCard(statsTable, bold, regular,
                "Total", total.ToString(),
                NavyBlue);
            AddStatCard(statsTable, bold, regular,
                "On Time", onTime.ToString(),
                SuccessGreen);
            AddStatCard(statsTable, bold, regular,
                "Late", late.ToString(),
                WarningOrange);
            AddStatCard(statsTable, bold, regular,
                "On-Time %", onTimePct + "%",
                LightBlue);

            doc.Add(statsTable);
        }

        private void AddStatCard(Table table, PdfFont bold,
            PdfFont regular, string label,
            string value, Color color)
        {
            var cell = new Cell()
                .SetBorder(new SolidBorder(color, 2f))
                .SetBorderBottom(new SolidBorder(color, 4f))
                .SetPadding(14)
                .SetTextAlignment(TextAlignment.CENTER);

            cell.Add(new Paragraph(value)
                .SetFont(bold)
                .SetFontSize(22)
                .SetFontColor(color)
                .SetMarginBottom(4));

            cell.Add(new Paragraph(label)
                .SetFont(regular)
                .SetFontSize(10)
                .SetFontColor(TextMuted));

            table.AddCell(cell);
        }

        private void AddAttendanceTable(Document doc,
            PdfFont bold, PdfFont regular,
            List<AttendanceRecord> records)
        {
            doc.Add(new Paragraph("Detailed Attendance")
                .SetFont(bold)
                .SetFontSize(13)
                .SetFontColor(NavyBlue)
                .SetMarginBottom(8));

            var table = new Table(
                UnitValue.CreatePercentArray(
                    new float[] { 0.5f, 2.5f, 1.5f, 1.5f, 1.2f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Header Row
            string[] headers = {
                "#", "Student Name",
                "Student ID", "Time", "Status" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .SetBackgroundColor(NavyBlue)
                    .SetBorder(Border.NO_BORDER)
                    .SetPaddingTop(8)
                    .SetPaddingBottom(8)
                    .SetPaddingLeft(8)
                    .SetPaddingRight(8)
                    .Add(new Paragraph(h)
                        .SetFont(bold)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.WHITE)));
            }

            // Data Rows
            int i = 1;
            foreach (var r in records.OrderBy(
                r => r.ScannedAt))
            {
                bool isEven = i % 2 == 0;
                var rowBg = isEven
                    ? LightGray
                    : ColorConstants.WHITE;

                var statusColor = r.Status ==
                    AttendanceStatus.OnTime
                    ? SuccessGreen : WarningOrange;

                string statusText = r.Status ==
                    AttendanceStatus.OnTime
                    ? "On Time" : "Late";

                AddTableCell(table, bold, regular,
                    i.ToString(), rowBg, false);
                AddTableCell(table, bold, regular,
                    r.User?.FullName ?? "-", rowBg, false);
                AddTableCell(table, bold, regular,
                    r.User?.StudentId ?? "-", rowBg, false);
                AddTableCell(table, bold, regular,
                    r.ScannedAt.ToString("HH:mm:ss"),
                    rowBg, false);

                // Status Cell مع لون خاص
                table.AddCell(new Cell()
                    .SetBackgroundColor(rowBg)
                    .SetBorder(Border.NO_BORDER)
                    .SetBorderBottom(new SolidBorder(
                        BorderGray, 0.3f))
                    .SetPadding(8)
                    .Add(new Paragraph(statusText)
                        .SetFont(bold)
                        .SetFontSize(9)
                        .SetFontColor(statusColor)));

                i++;
            }

            if (!records.Any())
            {
                var emptyCell = new Cell(1, 5)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph("No attendance records")
                        .SetFont(regular)
                        .SetFontSize(10)
                        .SetFontColor(TextMuted));
                table.AddCell(emptyCell);
            }

            doc.Add(table);
        }

        private void AddTableCell(Table table,
            PdfFont bold, PdfFont regular,
            string text, Color bg, bool isBold)
        {
            table.AddCell(new Cell()
                .SetBackgroundColor(bg)
                .SetBorder(Border.NO_BORDER)
                .SetBorderBottom(
                    new SolidBorder(BorderGray, 0.3f))
                .SetPadding(8)
                .Add(new Paragraph(text)
                    .SetFont(isBold ? bold : regular)
                    .SetFontSize(9)
                    .SetFontColor(TextDark)));
        }

        private void AddFooter(Document doc, PdfFont regular)
        {
            doc.Add(new LineSeparator(
                new iText.Kernel.Pdf.Canvas.Draw
                    .SolidLine(0.5f))
                .SetStrokeColor(BorderGray)
                .SetMarginTop(10)
                .SetMarginBottom(8));

            doc.Add(new Paragraph(
                $"Generated by QR Attendance System — " +
                $"{DateTime.Now:dd/MM/yyyy HH:mm} | " +
                "Faculty of Business Administration — EEIU")
                .SetFont(regular)
                .SetFontSize(8)
                .SetFontColor(TextMuted)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        // ============================================================
        // Student Report Helpers
        // ============================================================

        private void AddStudentHeader(Document doc,
            PdfFont bold, PdfFont regular, AppUser student)
        {
            var headerTable = new Table(1)
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            var cell = new Cell()
                .SetBackgroundColor(NavyBlue)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(20)
                .SetTextAlignment(TextAlignment.CENTER);

            cell.Add(new Paragraph(
                "Egyptian E-Learning University")
                .SetFont(bold).SetFontSize(14)
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginBottom(4));

            cell.Add(new Paragraph(
                "Student Attendance Report")
                .SetFont(bold).SetFontSize(18)
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginBottom(8));

            cell.Add(new Paragraph(student.FullName)
                .SetFont(regular).SetFontSize(13)
                .SetFontColor(
                    new DeviceRgb(200, 220, 240)));

            if (student.StudentId != null)
                cell.Add(new Paragraph(
                    "ID: " + student.StudentId)
                    .SetFont(regular).SetFontSize(11)
                    .SetFontColor(
                        new DeviceRgb(170, 200, 230)));

            headerTable.AddCell(cell);
            doc.Add(headerTable);
        }

        private void AddStudentCourseStats(Document doc,
            PdfFont bold, PdfFont regular,
            AppUser student,
            List<AttendanceRecord> records)
        {
            doc.Add(new Paragraph("Attendance by Course")
                .SetFont(bold).SetFontSize(13)
                .SetFontColor(NavyBlue)
                .SetMarginBottom(8));

            var groups = records
                .GroupBy(r => r.Session?.Course?.Name
                    ?? "Unknown")
                .ToList();

            var table = new Table(
                UnitValue.CreatePercentArray(
                    new float[] { 3f, 1f, 1f, 1f }))
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            foreach (var h in new[]{
                "Course", "Attended",
                "On Time", "Late" })
            {
                table.AddHeaderCell(new Cell()
                    .SetBackgroundColor(NavyBlue)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(8)
                    .Add(new Paragraph(h)
                        .SetFont(bold).SetFontSize(10)
                        .SetFontColor(
                            ColorConstants.WHITE)));
            }

            int rowIdx = 0;
            foreach (var g in groups)
            {
                var bg = rowIdx++ % 2 == 0
                    ? LightGray : ColorConstants.WHITE;
                int onT = g.Count(
                    r => r.Status ==
                        AttendanceStatus.OnTime);
                int lateC = g.Count(
                    r => r.Status ==
                        AttendanceStatus.Late);

                AddTableCell(table, bold, regular,
                    g.Key, bg, true);
                AddTableCell(table, bold, regular,
                    g.Count().ToString(), bg, false);
                AddTableCell(table, bold, regular,
                    onT.ToString(), bg, false);
                AddTableCell(table, bold, regular,
                    lateC.ToString(), bg, false);
            }

            doc.Add(table);
        }

        private void AddStudentRecordsTable(Document doc,
            PdfFont bold, PdfFont regular,
            List<AttendanceRecord> records)
        {
            doc.Add(new Paragraph("Full Attendance Log")
                .SetFont(bold).SetFontSize(13)
                .SetFontColor(NavyBlue)
                .SetMarginBottom(8));

            var table = new Table(
                UnitValue.CreatePercentArray(
                    new float[] { 0.5f, 2f, 1.5f, 1.5f, 1.2f }))
                .UseAllAvailableWidth();

            foreach (var h in new[]{
                "#", "Session", "Course",
                "Date/Time", "Status" })
            {
                table.AddHeaderCell(new Cell()
                    .SetBackgroundColor(LightBlue)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(8)
                    .Add(new Paragraph(h)
                        .SetFont(bold).SetFontSize(10)
                        .SetFontColor(
                            ColorConstants.WHITE)));
            }

            int i = 1;
            foreach (var r in records
                .OrderByDescending(r => r.ScannedAt))
            {
                var bg = i % 2 == 0
                    ? LightGray : ColorConstants.WHITE;
                var statusColor = r.Status ==
                    AttendanceStatus.OnTime
                    ? SuccessGreen : WarningOrange;

                AddTableCell(table, bold, regular,
                    i.ToString(), bg, false);
                AddTableCell(table, bold, regular,
                    r.Session?.Title ?? "-", bg, false);
                AddTableCell(table, bold, regular,
                    r.Session?.Course?.Name ?? "-",
                    bg, false);
                AddTableCell(table, bold, regular,
                    r.ScannedAt.ToString(
                        "dd/MM/yyyy HH:mm"),
                    bg, false);

                table.AddCell(new Cell()
                    .SetBackgroundColor(bg)
                    .SetBorder(Border.NO_BORDER)
                    .SetBorderBottom(new SolidBorder(
                        BorderGray, 0.3f))
                    .SetPadding(8)
                    .Add(new Paragraph(
                        r.Status ==
                        AttendanceStatus.OnTime
                        ? "On Time" : "Late")
                        .SetFont(bold)
                        .SetFontSize(9)
                        .SetFontColor(statusColor)));
                i++;
            }

            doc.Add(table);
        }
    }
}