#!/bin/bash

# Script untuk mencetak QR code yang mendukung panjang data fleksibel
# Dirancang khusus untuk printer Epson TM-T82X menggunakan ESC/POS commands

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
QR_TEXT="$1"
QR_SIZE="${2:-8}"  # Default ukuran 8 (medium), range 1-16

if [ -z "$QR_TEXT" ]; then
    echo "Error: Isi QR code tidak diberikan"
    echo "Penggunaan: $0 <qrcode_content> [qrcode_size]"
    echo "  qrcode_size: ukuran QR code (1-16, default: 8)"
    exit 1
fi

if [ "$QR_SIZE" -lt 1 ] || [ "$QR_SIZE" -gt 16 ]; then
    echo "Error: Ukuran QR code harus antara 1-16"
    exit 1
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# File untuk perintah ESC/POS
ESCPOS_FILE="$TEMP_DIR/qrcode_flexible.bin"

echo "Data QR: $QR_TEXT (${#QR_TEXT} karakter)"
echo "Ukuran QR: $QR_SIZE"

# Fungsi untuk menangani panjang data QR code
function qr_data_command() {
    local data="$1"
    local data_len=${#data}
    
    # QR code data command header: GS ( k pL pH cn fn m data
    printf "\x1D\x28\x6B" # GS ( k
    
    # Hitung pL dan pH berdasarkan panjang data
    # pL = (data_len + 3) % 256
    # pH = (data_len + 3) / 256
    local command_size=$((data_len + 3))
    local pL=$((command_size & 0xff))
    local pH=$((command_size >> 8))
    
    # Output pL dan pH sebagai bytes
    printf "\\$(printf '%03o' $pL)"
    printf "\\$(printf '%03o' $pH)"
    
    # cn fn m: 49 80 48 (31 50 30 dalam hex) = 1 P 0
    printf "\x31\x50\x30"
    
    # Tambahkan data QR code
    printf "%s" "$data"
}

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n\n"
    
    # ----- Teks header -----
    printf "QR CODE - FLEXIBLE\n\n"
    printf "Data: %s\n\n" "$QR_TEXT"
    
    # ----- QR Code Configuration -----
    # Set QR Code Model (model 2)
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Set QR Code Size (1-16)
    printf "\x1D\x28\x6B\x03\x00\x31\x43"
    printf "\\$(printf '%03o' $QR_SIZE)"
    
    # Set error correction level (L=0, M=1, Q=2, H=3)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"  # Level H (highest)
    
    # Store QR Code data dengan fungsi yang lebih fleksibel
    qr_data_command "$QR_TEXT"
    
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
echo "Mengirim perintah ESC/POS untuk QR code fleksibel ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 