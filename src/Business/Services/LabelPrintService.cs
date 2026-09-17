using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
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
        {
            var document = new FixedDocument();

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

                    var labelCanvas = BuildLabelCanvas(pageLabels[i], template, labelWidthPx, labelHeightPx);
                    FixedPage.SetLeft(labelCanvas, col * labelWidthPx);
                    FixedPage.SetTop(labelCanvas, row * labelHeightPx);
                    fixedPage.Children.Add(labelCanvas);
                }

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);
            }

            return document;
        }

        /// <summary>Builds the document and sends it to the named printer (or the system default if not found).</summary>
        public void PrintLabels(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string? printerName)
        {
            var document = BuildFixedDocument(jobs, template);
            var printDialog = new PrintDialog();

            if (!string.IsNullOrWhiteSpace(printerName))
            {
                try
                {
                    var printServer = new System.Printing.LocalPrintServer();
                    printDialog.PrintQueue = printServer.GetPrintQueue(printerName);
                }
                catch
                {
                    // Named printer not resolvable — fall back to the dialog's default queue
                }
            }

            printDialog.PrintDocument(document.DocumentPaginator, "EZPos Labels");
        }

        /// <summary>Lays out every job (expanded by its Quantity) into a PDF, one page per LabelsPerRow x LabelsPerColumn sheet.</summary>
        public void ExportToPdf(IEnumerable<LabelPrintJob> jobs, LabelTemplate template, string filePath)
        {
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
                    DrawLabelPdf(gfx, pageLabels[i], template, col * labelWidthPt, row * labelHeightPt, labelWidthPt, labelHeightPt);
                }
            }

            document.Save(filePath);
        }

        /// <summary>Names of all Windows printers currently installed on this machine.</summary>
        public List<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                printers.Add(name);
            return printers;
        }

        private Canvas BuildLabelCanvas(LabelPrintJob job, LabelTemplate template, double widthPx, double heightPx)
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

        private void DrawLabelPdf(XGraphics gfx, LabelPrintJob job, LabelTemplate template, double xPt, double yPt, double widthPt, double heightPt)
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
                var bytes = _barcodeService.GenerateImageBytes(job.Barcode, job.Format, 600, 300);
                using var stream = new MemoryStream(bytes);
                using var image = XImage.FromStream(() => stream);
                gfx.DrawImage(image, xPt + 2, y, Math.Max(0, widthPt - 4), barcodeHeightPt);
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
