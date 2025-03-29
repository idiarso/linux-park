# Barcode Printer Test Tools

Repositori ini berisi beberapa alat untuk menguji fungsionalitas printer barcode.

## Daftar Tool

1. **TestBarcode.csproj** - Project .NET untuk menguji pembuatan dan pencetakan barcode.
2. **test-barcode.cs** - Aplikasi console .NET untuk generasi barcode dan test print.
3. **print-barcode.sh** - Script shell untuk mencetak file gambar barcode.
4. **test-barcode-print.sh** - Script shell sederhana untuk mencetak tiket test.

## Persyaratan

- .NET SDK 6.0 atau lebih baru
- ImageMagick (`convert` command)
- CUPS untuk pencetakan
- Printer ESC/POS (Tested with TM-T82X-S-A)
- Paket-paket NuGet: 
  - ZXing.Net
  - ZXing.Net.Bindings.ImageSharp
  - SixLabors.ImageSharp

## Cara Penggunaan

### Menjalankan Test Barcode C#:

```bash
dotnet build TestBarcode.csproj
dotnet run --project TestBarcode.csproj
```

Ini akan:
1. Membuat barcode dengan format CODE-128 menggunakan ZXing
2. Menyimpan barcode sebagai PNG
3. Mencetak tiket test
4. Mencetak barcode menggunakan script shell

### Menggunakan Script Print Barcode:

```bash
./print-barcode.sh path/to/barcode/image.png
```

### Menggunakan Test Print Shell Script:

```bash
./test-barcode-print.sh
```

## Troubleshooting

Jika printer tidak berfungsi:

1. Periksa driver printer:
```bash
lpstat -p
```

2. Pastikan printer online:
```bash
lpstat -p TM-T82X-S-A
```

3. Periksa izin port USB:
```bash
sudo chmod 666 /dev/usb/lp0
```

4. Pastikan ImageMagick terinstal:
```bash
convert --version
```

## Integrasi dengan Aplikasi Utama

Tool test ini terpisah dari aplikasi utama, tetapi dapat diintegrasikan dengan menerapkan pola serupa di `PrinterService.cs`. Aplikasi utama menggunakan `IPrinterService` untuk mencetak tiket dan barcode. 