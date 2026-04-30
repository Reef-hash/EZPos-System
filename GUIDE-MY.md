# Panduan Penggunaan EZPos (Bahasa Malaysia)

EZPos ialah sistem POS mudah untuk kedai runcit kecil. Sistem ini mempunyai dua mod utama: Jualan (Sales Mode) dan Stok (Stock Mode).

---

## 1. Pemasangan & Persediaan

1. Pastikan .NET 6.0+ dan Visual Studio/VS Code telah dipasang.
2. Buka folder projek ini dalam Visual Studio/VS Code.
3. Restore NuGet package `System.Data.SQLite` jika belum ada.
4. Build dan jalankan projek.

---

## 2. Sales Mode (Mod Jualan)

- **Scan barcode** produk menggunakan scanner (atau taip manual dan tekan Enter).
- Jika produk dijumpai, ia akan ditambah ke cart.
- Jika tidak dijumpai, popup akan keluar.
- Cart paparkan senarai item, kuantiti, harga, dan jumlah.
- Boleh tambah/kurang kuantiti atau buang item (akan ditambah dalam versi akan datang).
- Masukkan jumlah tunai diterima.
- Tekan **BAYAR** untuk lengkapkan transaksi:
  - Jualan disimpan ke database.
  - Stok produk akan ditolak.
  - Resit ringkas akan dipaparkan (boleh diubah untuk cetakan ESC/POS sebenar).
- Tekan **CLEAR** untuk reset cart.

---

## 3. Stock Mode (Mod Stok)

- **Scan barcode** produk.
- Jika barcode wujud:
  - Papar maklumat produk (nama, stok, harga).
  - Masukkan kuantiti untuk tambah stok.
  - Tekan **Tambah Stok** untuk kemas kini.
- Jika barcode belum wujud:
  - Borang produk baru akan dipaparkan.
  - Masukkan nama, harga, dan stok permulaan.
  - Tekan **Simpan Produk Baru** untuk tambah ke database.

---

## 4. Nota Tambahan

- Semua data disimpan dalam fail SQLite `EZPos.db`.
- Untuk cetakan resit sebenar, integrasi ESC/POS printer diperlukan (boleh tambah kod pada fungsi PrintReceipt).
- Untuk skala SaaS, kod telah diasingkan secara modular (DataAccess, Models, BusinessLogic, UI).

---

## 5. Cadangan Penambahbaikan (untuk SaaS)

- Tambah login multi-user & cloud sync.
- Dashboard jualan harian.
- Amaran stok rendah.
- Carian produk manual.
- Integrasi pembayaran digital.

---

**Hubungi pembangun untuk sokongan atau penyesuaian lanjut.**
