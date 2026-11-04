using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using EZPos.DataAccess.Repositories;
using EZPos.Models.Domain;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace EZPos.Business.Services
{
    /// <summary>
    /// Builds printable label sheets and sends them to a WPF printer.
    /// Stateless aside from a private BarcodeService used to render each label's barcode image.
    /// References System.Windows.Controls for WPF printing only.
    /// </summary>
    public class LabelPrintService
    {
        private const double MmPerInch = 25.4;
        private const double PxPerInch = 96.0;
        private const double PtPerMm = 2.835;

        private readonly BarcodeService _barcodeService = new();

        /// <summary>Lays out every job (expanded by its Quantity) into pages of LabelsPerRow x LabelsPerColumn.</summary>
        public FixedDocument BuildFixedDocument(IEnumerable<LabelPrintJob> jobs, LabelTemplate template)
            => BuildFixedDocument(jobs, template, out _);

        /// <summary>
        /// Same as <see cref="BuildFixedDocument(IEnumerable{LabelPrintJob}, LabelTemplate)"/>, but also reports
        /// which jobs had barcode data that couldn't be encoded in their selected format — those labels render
        /// with a placeholder instead of aborting the whole document.
        /// </summary>
        public FixedDocument BuildFixedDocument(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, out List<LabelPrintJob> failedJobs)
        {
            var document = new FixedDocument();
            failedJobs = new List<LabelPrintJob>();

            var labelWidthPx = MmToPx(template.LabelWidthMm);
            var labelHeightPx = MmToPx(template.LabelHeightMm);
            var labelsPerRow = Math.Max(1, template.LabelsPerRow);
            var labelsPerColumn = Math.Max(1, template.LabelsPerColumn);
            var labelsPerPage = labelsPerRow * labelsPerColumn;

            var expandedLabels = jobs
                .SelectMany(job => Enumerable.Repeat(job, Math.Max(1, job.Quantity)))
                .ToList();

            if (expandedLabels.Count == 0)
                return document;

            for (var pageStart = 0; pageStart < expandedLabels.Count; pageStart += labelsPerPage)
            {
                var pageLabels = expandedLabels.Skip(pageStart).Take(labelsPerPage).ToList();

                var fixedPage = new FixedPage
                {
                    Width = labelWidthPx * labelsPerRow,
                    Height = labelHeightPx * labelsPerColumn
                };

                for (var i = 0; i < pageLabels.Count; i++)
                {
                    var row = i / labelsPerRow;
                    var col = i % labelsPerRow;

                    var labelCanvas = BuildLabelCanvas(pageLabels[i], template, labelWidthPx, labelHeightPx, failedJobs);
                    FixedPage.SetLeft(labelCanvas, col * labelWidthPx);
                    FixedPage.SetTop(labelCanvas, row * labelHeightPx);
                    fixedPage.Children.Add(labelCanvas);
                }

                var pageSize = new Size(fixedPage.Width, fixedPage.Height);
                fixedPage.Measure(pageSize);
                fixedPage.Arrange(new Rect(pageSize));
                fixedPage.UpdateLayout();

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);
            }

            return document;
        }

        /// <summary>Builds the document and sends it to the named printer (or the system default if not found).</summary>
        public void PrintLabels(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string? printerName)
            => PrintLabels(jobs, template, printerName, out _);

        /// <summary>
        /// Same as <see cref="PrintLabels(IEnumerable{LabelPrintJob}, LabelTemplate, string?)"/>, but also reports
        /// which jobs had unencodable barcode data (rendered as a placeholder instead of blocking the print job).
        /// </summary>
        public void PrintLabels(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string? printerName, out List<LabelPrintJob> failedJobs)
        {
            var document = BuildFixedDocument(jobs, template, out failedJobs);
            if (document.Pages.Count == 0)
                throw new InvalidOperationException("Nothing to print.");

            // Resolve the exact queue the user picked. Never silently fall back to another printer —
            // that used to send labels to the Windows default (often "Microsoft Print to PDF") while
            // the UI still reported success.
            var queue = ResolvePrintQueue(printerName);

            queue.Refresh();
            var problem = DescribeQueueProblem(queue);
            if (problem != null)
                throw new InvalidOperationException($"Printer '{queue.FullName}' is {problem}. Check the printer and Windows print queue, then try again.");

            // Ask the driver for a page that matches the label sheet instead of its default (A4/Letter),
            // otherwise label printers may hold the job waiting for the "right" media.
            var ticket = BuildPrintTicket(queue, template);

            queue.CurrentJobSettings.Description = "EZPos Labels";
            var writer = PrintQueue.CreateXpsDocumentWriter(queue);
            writer.Write(document.DocumentPaginator, ticket);

            // The spooler accepted the job — surface an obvious post-submit problem rather than reporting success.
            queue.Refresh();
            problem = DescribeQueueProblem(queue);
            if (problem != null)
                throw new InvalidOperationException($"Job was queued but printer '{queue.FullName}' is {problem}. Check the Windows print queue.");
        }

        /// <summary>Finds the print queue by name among local and connected (network/shared) printers; null/empty = system default.</summary>
        private static PrintQueue ResolvePrintQueue(string? printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                return LocalPrintServer.GetDefaultPrintQueue();

            var server = new LocalPrintServer();
            var queues = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });

            var match = queues.FirstOrDefault(q => string.Equals(q.FullName, printerName, StringComparison.OrdinalIgnoreCase))
                        ?? queues.FirstOrDefault(q => string.Equals(q.Name, printerName, StringComparison.OrdinalIgnoreCase));

            if (match == null)
                throw new InvalidOperationException($"Printer '{printerName}' was not found. Make sure it is installed and connected.");

            return match;
        }

        private static PrintTicket BuildPrintTicket(PrintQueue queue, LabelTemplate template)
        {
            var labelsPerRow = Math.Max(1, template.LabelsPerRow);
            var labelsPerColumn = Math.Max(1, template.LabelsPerColumn);
            var widthPx = MmToPx(template.LabelWidthMm) * labelsPerRow;
            var heightPx = MmToPx(template.LabelHeightMm) * labelsPerColumn;

            var baseTicket = queue.UserPrintTicket ?? queue.DefaultPrintTicket ?? new PrintTicket();
            var requested = baseTicket.Clone();
            requested.PageMediaSize = new PageMediaSize(widthPx, heightPx);
            requested.CopyCount = 1;

            try
            {
                var result = queue.MergeAndValidatePrintTicket(baseTicket, requested);
                return result.ValidatedPrintTicket ?? requested;
            }
            catch
            {
                // Some drivers reject custom sizes — print with their own settings rather than failing
                return baseTicket;
            }
        }

        /// <summary>Human-readable reason the queue cannot print right now, or null if it looks ready.</summary>
        private static string? DescribeQueueProblem(PrintQueue queue)
        {
            if (queue.IsOffline) return "offline (turn it on / reconnect the cable, and untick \"Use Printer Offline\" in Windows)";
            if (queue.IsPaused) return "paused in Windows";
            if (queue.IsNotAvailable) return "not available";
            if (queue.IsPaperJammed) return "jammed";
            if (queue.IsOutOfPaper) return "out of paper / labels";
            if (queue.IsDoorOpened) return "open (cover/door)";
            if (queue.IsInError) return "in an error state";
            if (queue.NeedUserIntervention) return "waiting for user attention";
            return null;
        }

        /// <summary>Lays out every job (expanded by its Quantity) into a PDF, one page per LabelsPerRow x LabelsPerColumn sheet.</summary>
        public void ExportToPdf(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string filePath)
            => ExportToPdf(jobs, template, filePath, out _);

        /// <summary>
        /// Same as <see cref="ExportToPdf(IEnumerable{LabelPrintJob}, LabelTemplate, string)"/>, but also reports
        /// which jobs had unencodable barcode data (rendered as a placeholder instead of blocking the export).
        /// </summary>
        public void ExportToPdf(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string filePath, out List<LabelPrintJob> failedJobs)
        {
            failedJobs = new List<LabelPrintJob>();
            var labelWidthPt = MmToPt(template.LabelWidthMm);
            var labelHeightPt = MmToPt(template.LabelHeightMm);
            var labelsPerRow = Math.Max(1, template.LabelsPerRow);
            var labelsPerColumn = Math.Max(1, template.LabelsPerColumn);
            var labelsPerPage = labelsPerRow * labelsPerColumn;

            var expandedLabels = jobs
                .SelectMany(job => Enumerable.Repeat(job, Math.Max(1, job.Quantity)))
                .ToList();

            if (expandedLabels.Count == 0)
                return;

            using var document = new PdfDocument();
            document.Info.Title = "EZPos Labels";

            for (var pageStart = 0; pageStart < expandedLabels.Count; pageStart += labelsPerPage)
            {
                var pageLabels = expandedLabels.Skip(pageStart).Take(labelsPerPage).ToList();

                var page = document.AddPage();
                page.Width = labelWidthPt * labelsPerRow;
                page.Height = labelHeightPt * labelsPerColumn;

                using var gfx = XGraphics.FromPdfPage(page);

                for (var i = 0; i < pageLabels.Count; i++)
                {
                    var row = i / labelsPerRow;
                    var col = i % labelsPerRow;
                    DrawLabelPdf(gfx, pageLabels[i], template, col * labelWidthPt, row * labelHeightPt, labelWidthPt, labelHeightPt, failedJobs);
                }
            }

            document.Save(filePath);
        }

        /// <summary>Names of all Windows printers currently installed on this machine.</summary>
        /// <summary>Windows default printer name, or null if none is set.</summary>
        public string? GetDefaultPrinterName()
        {
            try
            {
                var name = new System.Drawing.Printing.PrinterSettings().PrinterName;
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Picks the default printer if installed, otherwise the first one — avoids defaulting to "Fax" etc.</summary>
        public string? GetPreferredPrinter(IList<string> printers)
        {
            var def = GetDefaultPrinterName();
            return printers.FirstOrDefault(p => string.Equals(p, def, StringComparison.OrdinalIgnoreCase))
                   ?? printers.FirstOrDefault();
        }

        public List<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                printers.Add(name);
            return printers;
        }

        private Canvas BuildLabelCanvas(LabelPrintJob job, LabelTemplate template, double widthPx, double heightPx, List<LabelPrintJob> failedJobs)
        {
            var canvas = new Canvas
            {
                Width = widthPx,
                Height = heightPx,
                Background = Brushes.White
            };

            double y = 2;

            if (template.ShowStoreName)
            {
                var storeName = ConfigHelper.Get("StoreName", string.Empty);
                if (!string.IsNullOrWhiteSpace(storeName))
                {
                    AddCenteredText(canvas, storeName, widthPx, ref y, template.FontSizeName, FontWeights.Normal);
                }
            }

            if (template.ShowBarcode)
            {
                var barcodeHeightPx = heightPx * template.BarcodeHeightPct;

                if (_barcodeService.CanEncode(job.Barcode, job.Format, out _))
                {
                    var image = new Image
                    {
                        Source = _barcodeService.GenerateImage(job.Barcode, job.Format, (int)(widthPx * 3), (int)(barcodeHeightPx * 3)),
                        Width = Math.Max(0, widthPx - 4),
                        Height = barcodeHeightPx,
                        Stretch = Stretch.Fill
                    };
                    Canvas.SetLeft(image, 2);
                    Canvas.SetTop(image, y);
                    canvas.Children.Add(image);
                }
                else
                {
                    if (!failedJobs.Contains(job))
                        failedJobs.Add(job);
                    AddInvalidBarcodePlaceholder(canvas, widthPx, barcodeHeightPx, y);
                }

                y += barcodeHeightPx + 2;
            }

            if (template.ShowName)
            {
                AddCenteredText(canvas, job.ProductName, widthPx, ref y, template.FontSizeName, FontWeights.Normal);
            }

            if (template.ShowCategory)
            {
                AddCenteredText(canvas, job.Category, widthPx, ref y, template.FontSizeName * 0.9, FontWeights.Normal);
            }

            if (template.ShowPrice)
            {
                AddCenteredText(canvas, $"RM {job.Price:F2}", widthPx, ref y, template.FontSizePrice, FontWeights.Bold);
            }

            if (!string.IsNullOrWhiteSpace(template.CustomText))
            {
                AddCenteredText(canvas, template.CustomText!, widthPx, ref y, template.FontSizeName * 0.8, FontWeights.Normal);
            }

            return canvas;
        }

        private static void AddInvalidBarcodePlaceholder(Canvas canvas, double widthPx, double heightPx, double y)
        {
            var box = new Rectangle
            {
                Width = Math.Max(0, widthPx - 4),
                Height = heightPx,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            Canvas.SetLeft(box, 2);
            Canvas.SetTop(box, y);
            canvas.Children.Add(box);

            var text = new TextBlock
            {
                Text = "Invalid barcode",
                FontSize = 8,
                Foreground = Brushes.Red,
                TextAlignment = TextAlignment.Center,
                Width = widthPx
            };
            Canvas.SetLeft(text, 0);
            Canvas.SetTop(text, y + Math.Max(0, heightPx / 2 - 6));
            canvas.Children.Add(text);
        }

        private static void AddCenteredText(Canvas canvas, string text, double widthPx, ref double y, double fontSize, FontWeight weight)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = weight,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = widthPx
            };
            Canvas.SetLeft(textBlock, 0);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
            y += fontSize + 2;
        }

        private void DrawLabelPdf(XGraphics gfx, LabelPrintJob job, LabelTemplate template, double xPt, double yPt, double widthPt, double heightPt, List<LabelPrintJob> failedJobs)
        {
            var centerFormat = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Near };
            var nameFont = new XFont("Arial", template.FontSizeName, XFontStyle.Regular);
            var priceFont = new XFont("Arial", template.FontSizePrice, XFontStyle.Bold);
            var categoryFont = new XFont("Arial", template.FontSizeName * 0.9, XFontStyle.Regular);

            double y = yPt + 2;

            if (template.ShowStoreName)
            {
                var storeName = ConfigHelper.Get("StoreName", string.Empty);
                if (!string.IsNullOrWhiteSpace(storeName))
                {
                    gfx.DrawString(storeName, nameFont, XBrushes.Black, new XRect(xPt, y, widthPt, template.FontSizeName + 2), centerFormat);
                    y += template.FontSizeName + 2;
                }
            }

            if (template.ShowBarcode)
            {
                var barcodeHeightPt = heightPt * template.BarcodeHeightPct;

                if (_barcodeService.CanEncode(job.Barcode, job.Format, out _))
                {
                    var bytes = _barcodeService.GenerateImageBytes(job.Barcode, job.Format, 600, 300);
                    using var stream = new MemoryStream(bytes);
                    using var image = XImage.FromStream(() => stream);
                    gfx.DrawImage(image, xPt + 2, y, Math.Max(0, widthPt - 4), barcodeHeightPt);
                }
                else
                {
                    if (!failedJobs.Contains(job))
                        failedJobs.Add(job);

                    var pen = new XPen(XColors.Red, 1) { DashStyle = XDashStyle.Dash };
                    gfx.DrawRectangle(pen, xPt + 2, y, Math.Max(0, widthPt - 4), barcodeHeightPt);
                    gfx.DrawString("Invalid barcode", nameFont, XBrushes.Red, new XRect(xPt, y, widthPt, barcodeHeightPt), centerFormat);
                }

                y += barcodeHeightPt + 2;
            }

            if (template.ShowName)
            {
                gfx.DrawString(job.ProductName, nameFont, XBrushes.Black, new XRect(xPt, y, widthPt, template.FontSizeName + 2), centerFormat);
                y += template.FontSizeName + 2;
            }

            if (template.ShowCategory)
            {
                gfx.DrawString(job.Category, categoryFont, XBrushes.Black, new XRect(xPt, y, widthPt, template.FontSizeName + 2), centerFormat);
                y += template.FontSizeName + 2;
            }

            if (template.ShowPrice)
            {
                gfx.DrawString($"RM {job.Price:F2}", priceFont, XBrushes.Black, new XRect(xPt, y, widthPt, template.FontSizePrice + 2), centerFormat);
            }
        }

        private static double MmToPx(double mm) => mm / MmPerInch * PxPerInch;
        private static double MmToPt(double mm) => mm * PtPerMm;
    }
}
