#!/bin/bash

# Script untuk mencetak QR code sangat sederhana menggunakan ESC/POS commands
# Versi minimal dengan hanya elemen dasar

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
ESCPOS_FILE="$TEMP_DIR/qrcode_simple.bin"

echo "Data QR: $QR_TEXT (${#QR_TEXT} karakter)"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n\n"
    
    # ----- Teks header -----
    printf "QR Code - SIMPLE FORMAT\n\n"
    printf "Data: %s\n\n" "$QR_TEXT"
    
    # ----- QR Code Format -----
    # Set QR Code Model
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Set QR Code Size (1-16)
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x06"
    
    # Set error correction level
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x31"
    
    # Store QR Code data
    QR_LEN=${#QR_TEXT}
    QR_LEN_PLUS_3=$((QR_LEN + 3))
    QR_LEN_LOW=$((QR_LEN_PLUS_3 & 255))
    QR_LEN_HIGH=$((QR_LEN_PLUS_3 >> 8))
    
    # Cara manual yang lebih aman
    if [ "$QR_LEN_LOW" -eq 21 ]; then 
        printf "\x1D\x28\x6B\x15\x00\x31\x50\x30%s" "$QR_TEXT"
    elif [ "$QR_LEN_LOW" -eq 22 ]; then
        printf "\x1D\x28\x6B\x16\x00\x31\x50\x30%s" "$QR_TEXT"
    elif [ "$QR_LEN_LOW" -eq 23 ]; then
        printf "\x1D\x28\x6B\x17\x00\x31\x50\x30%s" "$QR_TEXT"
    elif [ "$QR_LEN_LOW" -eq 24 ]; then
        printf "\x1D\x28\x6B\x18\x00\x31\x50\x30%s" "$QR_TEXT"
    elif [ "$QR_LEN_LOW" -eq 25 ]; then
        printf "\x1D\x28\x6B\x19\x00\x31\x50\x30%s" "$QR_TEXT"
    else
        printf "\x1D\x28\x6B\x14\x00\x31\x50\x30%s" "$QR_TEXT"
    fi
    
    # Print QR Code
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Line feed untuk jarak
    printf "\n\n"
    
    # ----- Footer -----
    printf "======================\n\n"
    
    # Cut paper
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk QR code ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 