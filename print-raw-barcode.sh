#!/bin/bash

# Script untuk mencetak barcode langsung ke printer thermal
# menggunakan format bitmap yang kompatibel

# Ambil parameter
PRINTER_DEVICE="/dev/usb/lp0"
IMAGE_FILE="$1"

if [ -z "$IMAGE_FILE" ] || [ ! -f "$IMAGE_FILE" ]; then
    echo "Error: File gambar barcode tidak ditemukan"
    echo "Penggunaan: $0 <path_to_barcode_image>"
    exit 1
fi

# Cek izin device printer
if [ ! -w "$PRINTER_DEVICE" ]; then
    echo "Error: Tidak dapat menulis ke $PRINTER_DEVICE"
    echo "Coba jalankan: sudo chmod 666 $PRINTER_DEVICE"
    exit 1
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# Ubah ukuran dan optimalkan untuk printer thermal
echo "Mempersiapkan gambar untuk printer thermal..."
TEMP_IMG_FILE="$TEMP_DIR/barcode_raw.pbm"

# Proses gambar untuk printer thermal dengan format PBM (Portable Bitmap)
# Format ini sangat sederhana dan dapat dikirim hampir langsung ke printer
convert "$IMAGE_FILE" -resize 384x -gravity center -extent 384x192 -dither None -monochrome "$TEMP_IMG_FILE"

# Inisialisasi printer
echo "Inisialisasi printer..."
# ESC @ - Initialize printer
printf "\x1B\x40" > "$PRINTER_DEVICE"

# Center alignment
printf "\x1B\x61\x01" > "$PRINTER_DEVICE"

# Cetak header
echo -e "\n\n===== BARCODE TEST =====\n\n" > "$PRINTER_DEVICE"

# Kirim data gambar langsung ke printer
echo "Mengirim gambar ke printer..."
cat "$TEMP_IMG_FILE" > "$PRINTER_DEVICE"

# Line feed and cut
echo -e "\n\n\n\n\n\n" > "$PRINTER_DEVICE"
# GS V - Paper cut
printf "\x1D\x56\x00" > "$PRINTER_DEVICE"

echo "Selesai" 