# ESC/POS Barcode Printer Utility

Utility ini berisi script dan tools untuk mencetak barcode secara langsung menggunakan perintah ESC/POS standar pada printer thermal.

## Fitur Utama

1. Mencetak barcode CODE-128 langsung dari printer thermal
2. Mencetak QR code langsung dari printer thermal
3. Menggunakan perintah ESC/POS asli untuk hasil yang optimal
4. Tidak memerlukan gambar (PNG) - barcode dihasilkan oleh printer

## Keuntungan

- Barcode yang dihasilkan memiliki kualitas tinggi dan bisa dipindai
- Resolusi barcode sesuai dengan kemampuan printer
- Ukuran kertas tercetak lebih pendek dan efisien
- Proses lebih cepat karena tidak perlu mengirim gambar

## Script yang Tersedia

1. **print-escpos-barcode.sh** - Mencetak barcode CODE-128 menggunakan ESC/POS commands
2. **print-escpos-qrcode.sh** - Mencetak QR code menggunakan ESC/POS commands
3. **test-escpos-barcode.cs** - Aplikasi C# untuk menguji kedua jenis barcode

## Cara Penggunaan

### Mencetak Barcode CODE-128:

```bash
./print-escpos-barcode.sh "BARCODE-TEXT-CONTENT"
```

### Mencetak QR Code:

```bash
./print-escpos-qrcode.sh "URL-OR-TEXT-CONTENT"
```

### Menjalankan Aplikasi Test:

```bash
dotnet run --project BarcodeEscPos.csproj
```

## Perintah ESC/POS yang Digunakan

### Untuk Barcode CODE-128:
- `ESC @` - Reset printer
- `ESC !` - Set font size
- `ESC a` - Set alignment
- `GS h` - Set barcode height
- `GS w` - Set barcode width
- `GS H` - Set HRI position
- `GS f` - Set HRI font
- `GS k` - Print barcode

### Untuk QR Code:
- `GS ( k 04 00 31 41 32 00` - Select QR code model
- `GS ( k 03 00 31 43 06` - Set QR code size
- `GS ( k 03 00 31 45 31` - Set error correction level
- `GS ( k pL pH 31 50 30 data` - Store QR code data
- `GS ( k 03 00 31 51 30` - Print QR code

## Integrasi dengan Aplikasi

Untuk mengintegrasikan dengan aplikasi ParkIR yang ada, Anda dapat menerapkan logika serupa di dalam `PrinterService.cs` dengan memanggil perintah ESC/POS langsung, atau memodifikasi fungsi yang ada untuk menggunakan script-script ini.

## Dokumentasi Referensi

Untuk detail lengkap tentang perintah ESC/POS:
1. [ESC/POS Command Reference](https://reference.epson-biz.com/modules/ref_escpos/index.php)
2. [Epson TM-T88 Programming Guide](https://files.support.epson.com/pdf/pos/bulk/tm-t88iv_prog.pdf)
3. [QR Code Command Specifications](https://www.epson-biz.com/modules/ref_escpos/index.php?content_id=87) 