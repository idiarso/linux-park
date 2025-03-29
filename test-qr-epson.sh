#!/bin/bash

# QR Code Test Script - Format Epson
# Menggunakan format QR code Epson TM series yang lebih umum

PRINTER_NAME="TM-T82X-S-A"
QR_DATA="EPSON-TEST"

echo "Mengirim QR code format Epson ke printer $PRINTER_NAME..."
echo "Data QR: $QR_DATA"

# Buat file temporary
TEMP_FILE="/tmp/qr-epson.bin"

# Hitung panjang data
DATA_LEN=${#QR_DATA}
DATA_LEN_PLUS_3=$((DATA_LEN + 3))
LOWER_BYTE=$((DATA_LEN_PLUS_3 & 0xFF))
UPPER_BYTE=$((DATA_LEN_PLUS_3 >> 8))

echo "Data length: $DATA_LEN"
echo "Data length + 3: $DATA_LEN_PLUS_3"
echo "Lower byte: $LOWER_BYTE, Hex: $(printf '%02X' $LOWER_BYTE)"
echo "Upper byte: $UPPER_BYTE, Hex: $(printf '%02X' $UPPER_BYTE)"

# Tulis sequence ESC/POS ke file
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Teks header dengan font tebal
    printf "\x1B\x21\x08"
    printf "\n\nTEST QR CODE EPSON\n\n"
    printf "\x1B\x21\x00"
    printf "Format Epson TM\n\n"
    
    # ----- Barcode lurus sebagai tes -----
    # Cetak barcode CODE39 untuk verifikasi printer
    printf "\x1D\x6B\x04${QR_DATA}\x00"
    
    printf "\n\n"
    
    # ----- QR Code Format Epson TM -----
    # Inisialisasi
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Set ukuran QR code (6 = medium)
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x06"
    
    # Set error correction level (H = highest)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"
    
    # Store data dengan pL pL format
    printf "\x1D\x28\x6B$(printf "\\$(printf '%03o' $LOWER_BYTE)")$(printf "\\$(printf '%03o' $UPPER_BYTE)")\x31\x50\x30${QR_DATA}"
    
    # Print QR code
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Coba dengan metode lain jika yang pertama gagal
    printf "\n\nQR Code - Alt Method:\n\n"
    
    # Encode QR data dengan hex literal langsung
    # Model 2, size 6, error correct H
    # Ini format tetap panjang yang lebih sederhana
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x06" 
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"
    
    # Store data dengan fixed length 10 + 3
    DATA_FIXED="DIRECT-QR!"
    printf "\x1D\x28\x6B\x0D\x00\x31\x50\x30${DATA_FIXED}"
    
    # Print QR code
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Line feed dan cut
    printf "\n\n\n"
    printf "\x1D\x56\x01"
    
} > "$TEMP_FILE"

# Cetak dengan CUPS
lp -d "$PRINTER_NAME" -o raw "$TEMP_FILE"

# Tampilkan file juga secara hex untuk debugging
echo -e "\nFile HEX dump untuk debugging:"
hexdump -C "$TEMP_FILE" | head -20

echo "QR code terkirim ke printer!" 