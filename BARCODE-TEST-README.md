# Barcode Testing Toolkit

Toolkit untuk testing berbagai jenis barcode dan QR code pada printer thermal Epson TM-T82X menggunakan ESC/POS commands.

## Daftar Isi

1. [Pendahuluan](#pendahuluan)
2. [Instalasi](#instalasi)
3. [Penggunaan](#penggunaan)
4. [Script Tersedia](#script-tersedia)
5. [Aplikasi C#](#aplikasi-c)
6. [Troubleshooting](#troubleshooting)

## Pendahuluan

Toolkit ini dirancang untuk mempermudah pengujian pencetakan barcode dan QR code pada printer thermal Epson TM-T82X. Anda dapat mencetak berbagai jenis barcode dan QR code dengan berbagai ukuran untuk memastikan printer dan barcode scanner berfungsi dengan baik.

## Instalasi

1. Pastikan printer Epson TM-T82X sudah terpasang dan dikonfigurasi dengan benar dalam sistem
2. Pastikan printer memiliki nama `TM-T82X-S-A` di CUPS (atau edit script untuk menyesuaikan nama printer)
3. Pastikan .NET 6.0 atau yang lebih baru sudah terinstal
4. Buka terminal dan jalankan:

```bash
# Clone repo
git clone https://github.com/username/barcode-testing-toolkit.git
cd barcode-testing-toolkit

# Berikan permission executable
chmod +x *.sh
```

## Penggunaan

### Melalui Command Line

Anda dapat langsung menjalankan script bash untuk mencetak barcode atau QR code:

```bash
# Mencetak barcode CODE-128
./print-barcode-code128.sh "TIKET-12345"

# Mencetak QR code sederhana
./print-simple-qrcode.sh "TIKET-12345"

# Mencetak QR code ukuran besar
./print-qrcode-big.sh "TIKET-12345"

# Mencetak multiple QR code dengan berbagai ukuran
./print-qrcode-multiple.sh "TIKET-12345" "4,6,8,10"
```

### Melalui Aplikasi C#

Untuk testing yang lebih user-friendly, gunakan aplikasi C#:

```bash
dotnet run --project AllBarcodeTest.csproj
```

## Script Tersedia

Toolkit ini menyediakan beberapa script bash untuk mencetak berbagai jenis barcode:

1. **print-barcode-code128.sh** - Mencetak barcode CODE-128 standard
   - Format: `./print-barcode-code128.sh "TEXT_CONTENT"`

2. **print-simple-qrcode.sh** - Mencetak QR code dengan format sederhana
   - Format: `./print-simple-qrcode.sh "TEXT_CONTENT"`

3. **print-qrcode-big.sh** - Mencetak QR code dengan ukuran besar
   - Format: `./print-qrcode-big.sh "TEXT_CONTENT"`

4. **print-qrcode-multiple.sh** - Mencetak beberapa QR code dengan ukuran berbeda
   - Format: `./print-qrcode-multiple.sh "TEXT_CONTENT" [sizes]`
   - Dimana `sizes` adalah daftar ukuran yang dipisahkan koma, misal: "4,6,8,10"

5. **print-escpos-barcode.sh** - Script legacy untuk mencetak barcode (dipertahankan untuk kompatibilitas)
   - Format: `./print-escpos-barcode.sh "TEXT_CONTENT"`

## Aplikasi C#

Aplikasi C# menyediakan interface yang lebih user-friendly untuk mencetak barcode:

1. Build aplikasi:
   ```bash
   dotnet build AllBarcodeTest.csproj
   ```

2. Jalankan aplikasi:
   ```bash
   dotnet run --project AllBarcodeTest.csproj
   ```

3. Pilih opsi dari menu yang muncul:
   - 1: Cetak barcode CODE-128
   - 2: Cetak QR code sederhana
   - 3: Cetak QR code ukuran besar
   - 4: Cetak multiple QR code dengan berbagai ukuran
   - 5: Jalankan semua test sekaligus
   - 0: Keluar

## Troubleshooting

### QR Code Tidak Terbaca

Jika QR code tidak terbaca:
- Pastikan ukuran QR code cukup besar untuk scanner yang digunakan
- Coba gunakan error correction level yang lebih tinggi dengan mengedit script (ubah parameter 31-34)
- Pastikan printer tidak kehabisan tinta atau kertasnya tidak kusut

### Barcode CODE-128 Tidak Terbaca

Jika barcode CODE-128 tidak terbaca:
- Coba perbesar lebar barcode (ubah parameter GS w)
- Pastikan text di bawah barcode terbaca dengan jelas
- Periksa apakah karakter yang digunakan didukung oleh CODE-128

### Printer Error

Jika printer error:
- Periksa koneksi printer
- Pastikan nama printer benar di dalam script (`TM-T82X-S-A`)
- Periksa apakah printer didukung oleh ESC/POS commands yang digunakan

### Lokasi File Debug

Semua file ESC/POS commands disimpan di `/tmp/barcode-print/` untuk keperluan debugging. Anda dapat memeriksa file-file ini jika terjadi masalah. 