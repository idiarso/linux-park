#!/bin/bash

# QR Code Final Script - Khusus untuk TM-T82 series
PRINTER_NAME="TM-T82X-S-A"
QR_DATA="https://example.com"

echo "Printing QR Code with data: $QR_DATA"

# Buat temporary file untuk simpan binary data
rm -f /tmp/final-qr.bin

# Reset printer
printf "\x1B\x40" > /tmp/final-qr.bin

# Center alignment
printf "\x1B\x61\x01" >> /tmp/final-qr.bin

# Print header text
printf "\n\nFINAL QR TEST\n\n" >> /tmp/final-qr.bin

# ===== QR CODE =====
# Set QR Code model
printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00" >> /tmp/final-qr.bin 

# Set QR Code size
printf "\x1D\x28\x6B\x03\x00\x31\x43\x06" >> /tmp/final-qr.bin

# Set error correction level
printf "\x1D\x28\x6B\x03\x00\x31\x45\x31" >> /tmp/final-qr.bin

# Calculate data length
DATA_LENGTH=${#QR_DATA}
TOTAL_LENGTH=$((DATA_LENGTH + 3))
LOWER_BYTE=$((TOTAL_LENGTH & 255))
UPPER_BYTE=$((TOTAL_LENGTH >> 8))

echo "Data length: $DATA_LENGTH"
echo "pL (data length + 3): $TOTAL_LENGTH"
echo "pL byte (lower): $LOWER_BYTE (Hex: $(printf "%02X" $LOWER_BYTE))"
echo "pH byte (upper): $UPPER_BYTE (Hex: $(printf "%02X" $UPPER_BYTE))"

# Store QR code data
printf "\x1D\x28\x6B" >> /tmp/final-qr.bin
printf "\\x%02X\\x%02X" $LOWER_BYTE $UPPER_BYTE >> /tmp/final-qr.bin
printf "\x31\x50\x30" >> /tmp/final-qr.bin
printf "$QR_DATA" >> /tmp/final-qr.bin

# Print QR code
printf "\x1D\x28\x6B\x03\x00\x31\x51\x30" >> /tmp/final-qr.bin

# Feed and cut
printf "\n\n\n\n" >> /tmp/final-qr.bin
printf "\x1D\x56\x01" >> /tmp/final-qr.bin

# Check generated binary 
echo "Binary file created at /tmp/final-qr.bin"
echo "File content in hex:"
hexdump -C /tmp/final-qr.bin | head -10

echo "Sending to printer $PRINTER_NAME via CUPS..."

# Print using lp
lp -d "$PRINTER_NAME" -o raw /tmp/final-qr.bin

echo "QR code sent! Check printer for output." 