#!/bin/bash

# Script untuk mencetak barcode dengan format bitmap melalui CUPS

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
TEMP_IMG_FILE="$TEMP_DIR/barcode_bitmap.png"

# Proses gambar untuk printer thermal:
# 1. Resize ke lebar printer thermal (biasanya 72mm ~ 384px di 203dpi)
# 2. Tambahkan kontras agar barcode lebih jelas
# 3. Convert ke bitmap hitam-putih
convert "$IMAGE_FILE" -resize 384x -gravity center -extent 384x192 \
        -bordercolor white -border 10x10 \
        -contrast-stretch 0% \
        -dither None -monochrome \
        -density 203 \
        "$TEMP_IMG_FILE"

# Periksa hasil konversi
echo "Gambar telah diproses: $TEMP_IMG_FILE"
file "$TEMP_IMG_FILE"

# Cetak header sebagai text
TEMP_TXT_FILE="$TEMP_DIR/barcode_header.txt"
cat > "$TEMP_TXT_FILE" << EOF

===== TEST BARCODE =====

File: $(basename "$IMAGE_FILE")
Time: $(date "+%Y-%m-%d %H:%M:%S")

EOF

# Cetak header
echo "Mencetak header..."
lp -d "$PRINTER_NAME" -o raw "$TEMP_TXT_FILE"
sleep 1

# Cetak gambar
echo "Mencetak gambar barcode..."
lp -d "$PRINTER_NAME" -o raw "$TEMP_IMG_FILE"
sleep 1

# Cetak footer untuk membuat jarak dan memotong kertas
TEMP_FOOTER_FILE="$TEMP_DIR/barcode_footer.txt"
cat > "$TEMP_FOOTER_FILE" << EOF



======================



EOF

# Cetak footer
echo "Mencetak footer..."
lp -d "$PRINTER_NAME" -o raw "$TEMP_FOOTER_FILE"

# Bersihkan files (uncomment jika tidak perlu untuk debug)
# rm -f "$TEMP_IMG_FILE" "$TEMP_TXT_FILE" "$TEMP_FOOTER_FILE"

echo "Semua tugas cetak telah dikirim ke printer"
echo "File gambar yang dioptimalkan tersimpan di: $TEMP_IMG_FILE" 