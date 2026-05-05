using System;
using System.Runtime.InteropServices;

namespace EZPos.Utilities.Helpers
{
    /// <summary>
    /// Sends a raw byte buffer directly to a Windows printer via the winspool.drv API.
    /// This is required for ESC/POS thermal printers which need raw command bytes,
    /// not GDI-rendered pages.
    /// </summary>
    internal static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOW pDocInfo);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Sends <paramref name="data"/> to <paramref name="printerName"/> as a raw RAW-type job.
        /// Returns <c>true</c> on success.
        /// </summary>
        public static bool SendBytes(string printerName, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new ArgumentException("Printer name must not be empty.", nameof(printerName));

            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                throw new InvalidOperationException($"Cannot open printer '{printerName}'. Win32 error: {Marshal.GetLastWin32Error()}");

            try
            {
                var docInfo = new DOCINFOW
                {
                    pDocName    = "ESC/POS Receipt",
                    pOutputFile = null,
                    pDataType   = "RAW"
                };

                if (StartDocPrinter(hPrinter, 1, ref docInfo) == 0)
                    throw new InvalidOperationException($"StartDocPrinter failed. Win32 error: {Marshal.GetLastWin32Error()}");

                if (!StartPagePrinter(hPrinter))
                    throw new InvalidOperationException($"StartPagePrinter failed. Win32 error: {Marshal.GetLastWin32Error()}");

                var pData = Marshal.AllocCoTaskMem(data.Length);
                try
                {
                    Marshal.Copy(data, 0, pData, data.Length);
                    if (!WritePrinter(hPrinter, pData, data.Length, out _))
                        throw new InvalidOperationException($"WritePrinter failed. Win32 error: {Marshal.GetLastWin32Error()}");
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pData);
                }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                return true;
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
