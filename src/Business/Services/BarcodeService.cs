using System;
using System.IO;
using System.Windows.Media.Imaging;
using EZPos.DataAccess.Repositories;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using DomainBarcodeFormat = EZPos.Models.Domain.BarcodeFormat;

namespace EZPos.Business.Services
{
    /// <summary>
    /// Barcode image generation and internal-code helpers. Stateless — no WPF dependencies
    /// beyond the WPF imaging types needed to hand a renderable image back to the caller.
    /// </summary>
    public class BarcodeService
    {
        /// <summary>Renders a barcode value to a WPF-displayable image.</summary>
        public BitmapImage GenerateImage(string data, DomainBarcodeFormat format, int widthPx = 300, int heightPx = 150)
        {
            var bytes = GenerateImageBytes(data, format, widthPx, heightPx);

            var image = new BitmapImage();
            using (var stream = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        /// <summary>Renders a barcode value to raw PNG bytes — used by LabelPrintService for both WPF and PDF output.</summary>
        public byte[] GenerateImageBytes(string data, DomainBarcodeFormat format, int widthPx = 300, int heightPx = 150)
        {
            var writer = new BarcodeWriter
            {
                Format = MapFormat(format),
                Options = new EncodingOptions
                {
                    Width = widthPx,
                    Height = heightPx,
                    Margin = 4,
                    PureBarcode = false
                }
            };

            using var bitmap = writer.Write(data);
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Generates a guaranteed-unique, Code128-safe internal barcode for a product:
        /// EZP + zero-padded product Id (6 digits). Does not require a GS1 account.
        /// </summary>
        public string GenerateInternalCode(int productId)
        {
            return $"EZP{productId:D6}";
        }

        /// <summary>Returns true if no other product (excluding excludeProductId) already uses this barcode.</summary>
        public bool IsBarcodeUnique(string barcode, int excludeProductId)
        {
            var existing = ProductRepository.GetByBarcode(barcode);
            return existing == null || existing.Id == excludeProductId;
        }

        /// <summary>Validates a 13-digit EAN-13 value, including its check digit.</summary>
        public bool ValidateEan13(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 13)
                return false;

            var digits = new int[13];
            for (int i = 0; i < 13; i++)
            {
                if (!char.IsDigit(value[i]))
                    return false;
                digits[i] = value[i] - '0';
            }

            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += digits[i] * (i % 2 == 0 ? 1 : 3);

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == digits[12];
        }

        private static ZXing.BarcodeFormat MapFormat(DomainBarcodeFormat format) => format switch
        {
            DomainBarcodeFormat.Code39 => ZXing.BarcodeFormat.CODE_39,
            DomainBarcodeFormat.EAN13  => ZXing.BarcodeFormat.EAN_13,
            DomainBarcodeFormat.QR     => ZXing.BarcodeFormat.QR_CODE,
            _                          => ZXing.BarcodeFormat.CODE_128
        };
    }
}
