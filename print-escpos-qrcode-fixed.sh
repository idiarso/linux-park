#!/bin/bash

# Script untuk mencetak QR code langsung menggunakan ESC/POS commands
# Versi alternatif dengan format yang lebih kompatibel

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
QR_TEXT="$1"

if [ -z "$QR_TEXT" ]; then
    echo "Error: Isi QR code tidak diberikan"
    echo "Penggunaan: $0 <qrcode_content>"
    exit 1
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# File untuk perintah ESC/POS
ESCPOS_FILE="$TEMP_DIR/qrcode_escpos_fixed.bin"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # ESC ! - Font size normal
    printf "\x1B\x21\x00"
    
    # ESC a 1 - Center alignment
    printf "\x1B\x61\x01"
    
    # Line feeds and header
    printf "\n\n"
    printf "===== TEST QR CODE =====\n\n"
    printf "Data: %s\n" "$QR_TEXT"
    printf "Waktu: $(date '+%d/%m/%Y %H:%M:%S')\n\n"
    
    # ----------------------
    # Metode 1: Format EPSON asli
    # ----------------------
    
    # Model QR
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Ukuran QR - Coba ukuran lebih besar: 8
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x08"
    
    # Error correction - Level M (15%)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x32"
    
    # Simpan data - Format berbeda dan lebih sederhana
    QR_LEN=${#QR_TEXT}
    QR_LEN_PLUS_3=$((QR_LEN + 3))
    QR_LEN_LOW=$((QR_LEN_PLUS_3 & 255))
    QR_LEN_HIGH=$((QR_LEN_PLUS_3 >> 8))
    
    printf "\x1D\x28\x6B"
    printf "\\$(printf '%03o' "$QR_LEN_LOW")"
    printf "\\$(printf '%03o' "$QR_LEN_HIGH")"
    printf "\x31\x50\x30"
    printf "%s" "$QR_TEXT"
    
    # Cetak QR
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Tambahkan sedikit jarak 
    printf "\n\n"
    
    # ----------------------
    # Metode 2: Format alternatif - Karena printer berbeda
    # Gunakan ini jika metode 1 tidak bekerja
    # ----------------------
    
    # Tambahkan label untuk membedakan metode
    printf "\n--- METODE ALTERNATIF ---\n\n"
    
    # Gunakan versi sederhana (hanya untuk printer tertentu)
    printf "\x1D\x5A\x02"  # Pilih jenis QR
    printf "\x1D\x5A\x03"  # Ukuran besar
    printf "\x1D\x5A\x08"  # Error correction
    
    # Lebar QR
    printf "\x1D\x57\x02\x01"
    
    # Kirim data QR
    printf "\x1D\x5A\x01"
    printf "%s" "$QR_TEXT"
    printf "\x00"  # Null terminator
    
    # Cetak QR
    printf "\x1D\x5A\x00"
    
    # Line feeds and footer
    printf "\n\n"
    printf "======================\n\n"
    
    # GS V - Paper cut (partial)
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk QR code ke printer (versi fixed)..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 