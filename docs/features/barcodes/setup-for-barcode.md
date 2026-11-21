# Setup Printer Barcode (Xprinter XP-365B) di PC Client

> Panduan untuk juruteknik yang memasang EZPos + printer label barcode di kedai client.
> Sudah diuji pada Windows 11 dengan Xprinter XP-365B (USB), driver Seagull v11.5.0.

---

## Ringkasan

| Langkah | Apa | Wajib? |
|---|---|---|
| 1 | Sambung & calibrate printer | Ya |
| 2 | Pasang driver secara manual (bukan auto-detect) | Ya |
| 3 | Set saiz kertas dalam driver | Disyorkan |
| 4 | Pilih printer & template dalam EZPos | Ya |

---

## 1. Sambung printer

1. Sambung kabel USB ke PC, kemudian hidupkan printer.
2. Muatkan gulungan label.
3. **Calibrate:** tekan dan tahan butang **FEED** sehingga lampu berkelip, kemudian lepaskan. Printer akan keluarkan beberapa label dan berhenti di permulaan label.

---

## 2. Pasang driver

> ⚠️ **Jangan guna auto-detect USB dalam wizard driver Seagull.** Pakej driver tiada model
> "XP-365B" (hanya ada XP-D365B). Printer melaporkan hardware ID `USBPRINT\XprinterXP-365B6494`,
> jadi wizard tak jumpa padanan dan **asyik loading tanpa henti**.

### 2.1 Muat turun & ekstrak

1. Muat turun driver Seagull untuk Xprinter:
   https://www.seagullscientific.com/downloads/printer-drivers/xprinter-xp-365b
   (atau laman rasmi Xprinter: https://www.xprintertech.com/drivers-2)
2. Ekstrak fail `.exe` **tanpa menjalankannya**. Buka Command Prompt dalam folder fail itu, kemudian taip:

   ```cmd
   mkdir C:\Drivers\Xprinter
   tar -xf ExpressDelivery_11.5.0.exe -C C:\Drivers\Xprinter
   ```

   Nombor versi dalam nama fail mungkin berbeza. Guna nama fail yang dimuat turun.
   Selepas ekstrak, mesti ada fail `C:\Drivers\Xprinter\Xprinter.inf`.

### 2.2 Tambah printer secara manual

1. **Control Panel → Devices and Printers → Add a printer**.
2. Klik **"The printer that I want isn't listed"**.
3. Pilih **"Add a local printer or network printer with manual settings"**.
4. Pilih **"Use an existing port"**, kemudian pilih **`USB001`** (Virtual printer port for USB).
   - Kalau ada beberapa port `USB00x`, cabut dan pasang semula kabel printer. Port yang berkaitan dengan printer ialah yang muncul selepas itu.
5. Klik **Have Disk…**, kemudian Browse ke `C:\Drivers\Xprinter\Xprinter.inf`.
6. Pilih model **Xprinter XP-D365B**. Model ini serasi dengan XP-365B (siri 80mm dan bahasa TSPL yang sama).
7. Namakan printer **`Xprinter XP-365B`**. Jangan share.
8. Klik **Print a test page** untuk pastikan printer berfungsi.

### 2.3 (Alternatif) Pasang dengan PowerShell

Jalankan PowerShell sebagai **Administrator**:

```powershell
pnputil.exe /add-driver "C:\Drivers\Xprinter\Xprinter.inf" /install
Add-PrinterDriver -Name "Xprinter XP-D365B"
Add-Printer -Name "Xprinter XP-365B" -DriverName "Xprinter XP-D365B" -PortName "USB001"
Get-Printer | Format-Table Name, DriverName, PortName
```

---

## 3. Set saiz kertas (disyorkan)

Saiz lalai driver ialah **76 × 130 mm**. Dari v1.7.3 EZPos meminta saiz label melalui `PrintTicket`, tetapi sesetengah driver menolak saiz tersuai dan guna saiz lalainya (lihat
[Isu diketahui](#isu-diketahui)). Jadi tetapkan saiz di sini untuk pastikan tiada label kosong keluar
dan isi label tak tercetak di penjuru.

1. **Settings → Printers & scanners → Xprinter XP-365B → Printing preferences**.
2. Tab **Page Setup**, klik **New…** di bahagian *Stock*.
3. Masukkan saiz label sebenar (contohnya **40 × 30 mm**). Jenis media: **Labels with Gaps**.
4. Klik **OK**, kemudian **Apply**.

Saiz ini mesti sama dengan saiz template yang dipilih dalam EZPos:

| Template EZPos | Saiz label |
|---|---|
| Standard 40x30 | 40 × 30 mm |
| Price Tag 58x40 | 58 × 40 mm |
| Shelf Label 100x50 | 100 × 50 mm (lebih lebar daripada printer 80mm, tak sesuai untuk XP-365B) |

---

## 4. Setup dalam EZPos

1. Pasang EZPos dan aktifkan lesen.
2. Kalau EZPos sudah terbuka masa printer dipasang, **tutup dan buka semula** supaya senarai printer dimuat semula.
3. Buka halaman **Barcodes**:
   - **Printer:** pilih **Xprinter XP-365B**.
   - **Template:** pilih template yang sama saiz dengan label (langkah 3).
   - **Format:** Code128 (default, sesuai untuk kod dalaman `EZP000001`). Guna EAN13 hanya untuk barcode 12/13 digit yang sah.
4. Pilih satu produk, klik **Print Labels**, kemudian **scan** label yang keluar untuk pastikan barcode boleh dibaca.

---

## Troubleshooting

| Masalah | Punca | Penyelesaian |
|---|---|---|
| Wizard driver asyik loading | Auto-detect tak kenal XP-365B | Pasang manual (langkah 2.2) |
| Printer tiada dalam dropdown EZPos | Driver belum dipasang / EZPos belum restart | Semak dalam *Printers & scanners*, kemudian restart EZPos |
| Tekan Print tapi tiada apa-apa keluar | Dropdown pilih printer lain (contohnya PDF) | Pilih Xprinter XP-365B dalam dropdown. EZPos paparkan mesej jika printer offline atau kehabisan label |
| Beberapa label kosong keluar | Saiz kertas driver tak sama dengan label | Set saiz kertas (langkah 3), kemudian calibrate semula |
| Isi label di penjuru / terpotong | Saiz template ≠ saiz label | Pilih template yang betul |
| Kotak merah "Invalid barcode" | Barcode produk tak sah untuk format dipilih | Tukar format ke Code128 atau betulkan barcode produk |
| Barcode tak boleh di-scan | Label terlalu kecil / format salah | Guna template lebih besar, semak format |

---

## Isu diketahui

Status berdasarkan EZPos **v1.7.3**.

**Sudah dibaiki dalam v1.7.3** (tak perlu workaround, tetapi kekalkan langkah 3 sebagai langkah selamat):
- Printer yang dipilih dalam dropdown digunakan dengan tepat. Tiada lagi fallback senyap ke printer lain.
- Status printer (offline, paused, kehabisan label, jammed) disemak dan dipaparkan, bukannya mesej "Sent" palsu.
- Saiz label dihantar ke printer melalui `PrintTicket`. Kalau driver menolak saiz tersuai, EZPos guna tetapan driver sendiri, jadi langkah 3 tetap perlu.
- Dropdown Printer default kepada printer default Windows, bukan printer pertama dalam senarai.

**Masih ada:**
1. **Pilihan printer tak diingat.** Dropdown reset kepada printer default Windows setiap kali halaman dibuka. Kalau printer default Windows ialah printer lain (contohnya PDF), client mesti pilih semula Xprinter XP-365B, atau tetapkan Xprinter sebagai *default printer* dalam Windows (Printers & scanners → Xprinter XP-365B → Set as default).
2. **Template "A4 Sheet (24 labels)" lebih lebar daripada A4.** 4 × 70mm = 280mm > 210mm, jadi lajur ke-4 terpotong (sepatutnya 3 × 8).
3. **Format barcode produk diabaikan di halaman Barcodes.** Semua label guna format dropdown halaman, bukan `BarcodeFormat` produk itu sendiri.
4. **`CanEncode()` belum lengkap.** Code39 lebih 80 aksara masih boleh menggagalkan seluruh batch. Code39 huruf kecil ditolak walaupun ZXing boleh encode.
