#!/bin/bash

# QR Code Simple Script - Khusus untuk TM-T82 series
PRINTER_NAME="TM-T82X-S-A"
QR_DATA="https://example.com"

echo "Printing Simple QR Code with data: $QR_DATA"

# Buat temporary file untuk simpan binary data
rm -f /tmp/simple-qr.bin
touch /tmp/simple-qr.bin

# Reset printer
echo -ne "\x1B\x40" >> /tmp/simple-qr.bin

# Center alignment
echo -ne "\x1B\x61\x01" >> /tmp/simple-qr.bin

# Print header text
echo -ne "\n\nSIMPLE QR TEST\n\n" >> /tmp/simple-qr.bin

# ===== QR CODE =====
# Standar format: GS ( k pL pH cn fn [parameters]
# pL pH: parameter length
# cn: 49 decimal (0x31 hex) for QR
# fn: function number (65=model, 67=size, 69=error correction, 80=store data, 81=print)

# GS ( k pL pH cn fn [parameters] - Set QR Code model (fn=65)
echo -ne "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00" >> /tmp/simple-qr.bin 
# Model 2, with a trailing 0

# GS ( k pL pH cn fn n - Set QR Code size (fn=67)
echo -ne "\x1D\x28\x6B\x03\x00\x31\x43\x05" >> /tmp/simple-qr.bin
# Size 5 (5×5 dots per module)

# GS ( k pL pH cn fn n - Set error correction level (fn=69)
echo -ne "\x1D\x28\x6B\x03\x00\x31\x45\x31" >> /tmp/simple-qr.bin
# Error correction level 1 (7%)

# Calculate data length (need correct pL pH values)
DATA_LENGTH=${#QR_DATA}
TOTAL_LENGTH=$((DATA_LENGTH + 3))
LOWER_BYTE=$((TOTAL_LENGTH & 255))
UPPER_BYTE=$((TOTAL_LENGTH >> 8))

echo "Data length: $DATA_LENGTH"
echo "pL (data length + 3): $TOTAL_LENGTH"
echo "pL byte (lower): $LOWER_BYTE (Hex: $(printf "%02X" $LOWER_BYTE))"
echo "pH byte (upper): $UPPER_BYTE (Hex: $(printf "%02X" $UPPER_BYTE))"

# GS ( k pL pH cn fn m d1...dk - Store symbol data (fn=80)
echo -ne "\x1D\x28\x6B" >> /tmp/simple-qr.bin
echo -ne $(printf "\\x%02X\\x%02X" $LOWER_BYTE $UPPER_BYTE) >> /tmp/simple-qr.bin
echo -ne "\x31\x50\x30" >> /tmp/simple-qr.bin
echo -ne "$QR_DATA" >> /tmp/simple-qr.bin

# GS ( k pL pH cn fn m - Print symbol data (fn=81)
echo -ne "\x1D\x28\x6B\x03\x00\x31\x51\x30" >> /tmp/simple-qr.bin

# Feed and cut
echo -ne "\n\n\n\n" >> /tmp/simple-qr.bin
echo -ne "\x1D\x56\x01" >> /tmp/simple-qr.bin

# Check generated binary 
echo "Binary file created at /tmp/simple-qr.bin"
echo "Sending to printer $PRINTER_NAME via CUPS..."

# Print using lp
lp -d "$PRINTER_NAME" -o raw /tmp/simple-qr.bin

echo "QR code sent! Check printer for output." 