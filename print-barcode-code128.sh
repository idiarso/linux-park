#!/bin/bash

# Script untuk mencetak barcode CODE-128 menggunakan ESC/POS commands

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
BARCODE_TEXT="$1"

if [ -z "$BARCODE_TEXT" ]; then
    echo "Error: Isi barcode tidak diberikan"
    echo "Penggunaan: $0 <barcode_content>"
    exit 1
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# File untuk perintah ESC/POS
ESCPOS_FILE="$TEMP_DIR/barcode_code128.bin"

echo "Data Barcode: $BARCODE_TEXT (${#BARCODE_TEXT} karakter)"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n"
    
    # ----- Teks header -----
    printf "BARCODE CODE-128\n\n"
    printf "Data: %s\n\n" "$BARCODE_TEXT"
    
    # ----- Barcode Format -----
    # GS h - Set barcode height - 100 dots
    printf "\x1D\x68\x64"
    
    # GS w - Set barcode width (2-6) - width 3
    printf "\x1D\x77\x03"
    
    # GS H - Set barcode text position (0=none, 1=above, 2=below, 3=both)
    printf "\x1D\x48\x02"
    
    # GS k - Print barcode (code 73 for CODE128)
    printf "\x1D\x6B\x49"
    
    # Barcode data length
    printf "\\$(printf '%03o' "${#BARCODE_TEXT}")"
    
    # Barcode data
    printf "%s" "$BARCODE_TEXT"
    
    # Line feed untuk jarak
    printf "\n\n"
    
    # ----- Footer -----
    printf "======================\n\n"
    
    # Cut paper
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk barcode CODE-128 ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 