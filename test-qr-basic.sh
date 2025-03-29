#!/bin/bash

# QR Code Test Script - Versi Basic 
# Dirancang khusus untuk TM-T82X ESC/POS printer

PRINTER_NAME="TM-T82X-S-A"
QR_DATA="Test123"
QR_SIZE=4  # Ukuran QR (1-16)

echo "Mengirim QR code paling dasar ke printer $PRINTER_NAME..."
echo "Data QR: $QR_DATA"
echo "Ukuran QR: $QR_SIZE"

# Buat file temporary
TEMP_FILE="/tmp/qr-basic.bin"

# Tulis sequence ESC/POS ke file
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Teks header
    printf "\n\nTEST QR CODE\n\n"
    printf "Format paling dasar\n\n"
    
    # ----- QR Code Format Standar -----
    # Model: 2 (default untuk sebagian besar printer)
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Size: 4 (1-16)
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x04"
    
    # Error correction: Level H (3)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"
    
    # Store data: Dalam contoh ini menggunakan "Test123" (7 karakter + 3 = 10)
    printf "\x1D\x28\x6B\x0A\x00\x31\x50\x30$QR_DATA"
    
    # Print QR code
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Line feed
    printf "\n\n"
    
    # Coba lagi dengan ukuran 8
    printf "QR Code ukuran 8:\n\n"
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    printf "\x1D\x28\x6B\x03\x00\x31\x43\x08"
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"
    printf "\x1D\x28\x6B\x0A\x00\x31\x50\x30$QR_DATA"
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Line feed dan cut
    printf "\n\n\n"
    printf "\x1D\x56\x01"
    
} > "$TEMP_FILE"

# Cetak dengan CUPS
lp -d "$PRINTER_NAME" -o raw "$TEMP_FILE"

echo "QR code terkirim ke printer!"
echo "File binary QR code tersimpan di: $TEMP_FILE" 