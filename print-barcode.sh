#!/bin/bash

# Script untuk mencetak barcode menggunakan printer ESC/POS

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
IMAGE_FILE="$1"

if [ -z "$IMAGE_FILE" ] || [ ! -f "$IMAGE_FILE" ]; then
    echo "Error: File gambar barcode tidak ditemukan"
    echo "Penggunaan: $0 <path_to_barcode_image>"
    exit 1
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# Ubah ukuran dan optimalkan untuk printer thermal
echo "Mempersiapkan gambar untuk printer thermal..."
TEMP_IMG_FILE="$TEMP_DIR/barcode_processed.png"

# Proses gambar untuk printer thermal:
# 1. Resize ke lebar printer thermal (biasanya 72mm = ~720px di 300dpi)
# 2. Tambahkan kontras agar barcode lebih jelas
# 3. Convert ke grayscale 1-bit untuk printer thermal
convert "$IMAGE_FILE" -resize 576x -bordercolor white -border 20x10 -dither None -monochrome "$TEMP_IMG_FILE"

# Periksa hasil konversi
echo "Gambar telah diproses: $TEMP_IMG_FILE"
file "$TEMP_IMG_FILE"

# Konversi ke format PS untuk dicetak
echo "Mengkonversi ke format yang didukung printer..."
TEMP_PS_FILE="$TEMP_DIR/barcode_print.ps"
convert "$TEMP_IMG_FILE" -density 203 -page 80x40mm -gravity center "$TEMP_PS_FILE"

# Cetak file
echo "Mengirim ke printer $PRINTER_NAME..."
lp -d "$PRINTER_NAME" "$TEMP_PS_FILE"

# Debug: tampilkan perintah cetak alternatif
echo ""
echo "Alternatif perintah cetak langsung untuk debug:"
echo "cat $TEMP_PS_FILE > /dev/usb/lp0"

# Bersihkan
# Komentar baris di bawah jika ingin memeriksa file yang dihasilkan
# rm -f "$TEMP_IMG_FILE" "$TEMP_PS_FILE"
echo "Selesai" 