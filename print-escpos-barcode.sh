#!/bin/bash

# Script untuk mencetak barcode langsung menggunakan ESC/POS commands
# Ini menghasilkan barcode yang sebenarnya (yang bisa di-scan)

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
ESCPOS_FILE="$TEMP_DIR/barcode_escpos.bin"

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
    printf "===== TEST BARCODE =====\n\n"
    printf "Tiket: %s\n" "$BARCODE_TEXT"
    printf "Waktu: $(date '+%d/%m/%Y %H:%M:%S')\n\n"
    
    # GS h - Set barcode height: 80 dots
    printf "\x1D\x68\x50"
    
    # GS w - Set barcode width: 3 (medium)
    printf "\x1D\x77\x03"
    
    # GS H - Set HRI position: 2 (below)
    printf "\x1D\x48\x02"
    
    # GS f - Set HRI font: 0 (normal)
    printf "\x1D\x66\x00"
    
    # GS k - Print CODE128 barcode
    # 73 = CODE128, len = length of data, then data
    BARCODE_LEN=${#BARCODE_TEXT}
    printf "\x1D\x6B\x49\x${BARCODE_LEN}%s" "$BARCODE_TEXT"
    
    # Line feeds and footer
    printf "\n\n"
    printf "======================\n\n"
    
    # GS V - Paper cut (partial)
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk barcode ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 