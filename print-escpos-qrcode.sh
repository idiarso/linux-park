#!/bin/bash

# Script untuk mencetak QR code langsung menggunakan ESC/POS commands
# Ini menghasilkan QR code yang sebenarnya (yang bisa di-scan)

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
ESCPOS_FILE="$TEMP_DIR/qrcode_escpos.bin"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # ESC ! - Font size normal
    printf "\x1B\x21\x00"
    
    # ESC a 1 - Center alignment
    printf "\x1B\x61\x01"
    
    # Line feeds and header
    printf "\n"
    printf "===== TEST QR CODE =====\n\n"
    printf "Data: %s\n" "$QR_TEXT"
    printf "Waktu: $(date '+%d/%m/%Y %H:%M:%S')\n\n"
    
    # QR Code: Model 2
    # GS ( k - QR Code: Function 165 (model)
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00" # model 2, size = 0 (auto)
    
    # QR Code: Size
    # GS ( k - QR Code: Function 167 (size)
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x06" # size = 6 (medium)
    
    # QR Code: Error correction
    # GS ( k - QR Code: Function 169 (error correction)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x31" # error correction = 1 (7%)
    
    # QR Code: Store data
    # GS ( k - QR Code: Function 180 (store data)
    QR_LEN=${#QR_TEXT}
    # Calculate low and high bytes for QR data length plus 3
    QR_LEN_PLUS_3=$((QR_LEN + 3))
    QR_LEN_LOW=$((QR_LEN_PLUS_3 & 255))
    QR_LEN_HIGH=$((QR_LEN_PLUS_3 >> 8))
    printf "\x1D\x28\x6B$(printf "\\$(printf '%03o' "$QR_LEN_LOW")")$(printf "\\$(printf '%03o' "$QR_LEN_HIGH")")\x31\x50\x30%s" "$QR_TEXT"
    
    # QR Code: Print
    # GS ( k - QR Code: Function 181 (print)
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Line feeds and footer
    printf "\n\n"
    printf "======================\n\n"
    
    # GS V - Paper cut (partial)
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk QR code ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 