#!/bin/bash

# QR Code Script (Hex Method) - Untuk printer Epson TM-T82
PRINTER_NAME="TM-T82X-S-A"
QR_DATA="https://example.com"

echo "Printing QR Code dengan data: $QR_DATA"

# Buat hexdump untuk QR code - lebih reliable daripada echo
cat > /tmp/qr-hex.txt << 'EOF'
1B 40
1B 61 01
0A 0A 51 52 20 43 4F 44 45 20 54 45 53 54 0A 0A

# QR Code Model
1D 28 6B 04 00 31 41 32 00

# QR Code Size (8)
1D 28 6B 03 00 31 43 08

# Error correction (High)
1D 28 6B 03 00 31 45 33
EOF

# Calculate data length for QR store command
DATA_LENGTH=${#QR_DATA}
TOTAL_LENGTH=$((DATA_LENGTH + 3))
LOWER_BYTE=$((TOTAL_LENGTH & 255))
UPPER_BYTE=$((TOTAL_LENGTH >> 8))

echo "Data length: $DATA_LENGTH"
echo "pL (data length + 3): $TOTAL_LENGTH"
echo "pL byte (lower): $LOWER_BYTE (Hex: $(printf "%02X" $LOWER_BYTE))"
echo "pH byte (upper): $UPPER_BYTE (Hex: $(printf "%02X" $UPPER_BYTE))"

# Add store QR data command with calculated length
echo -e "# Store QR Data\n1D 28 6B $(printf "%02X" $LOWER_BYTE) $(printf "%02X" $UPPER_BYTE) 31 50 30" >> /tmp/qr-hex.txt

# Add the data as hex
for (( i=0; i<${#QR_DATA}; i++ )); do
    printf "%02X " "'${QR_DATA:$i:1}" >> /tmp/qr-hex.txt
done
echo "" >> /tmp/qr-hex.txt

# Add print command and feed/cut
cat >> /tmp/qr-hex.txt << 'EOF'
# Print QR Code
1D 28 6B 03 00 31 51 30

# Feed and cut
0A 0A 0A 0A
1D 56 00
EOF

# Convert hex to binary
xxd -r -p < <(grep -v "^#" /tmp/qr-hex.txt | tr -d '\n ') > /tmp/qr-hex.bin

# Check file
echo "Binary file created at /tmp/qr-hex.bin"
echo "File content in hex:"
hexdump -C /tmp/qr-hex.bin | head -10

echo "Sending to printer $PRINTER_NAME via CUPS..."

# Print using lp
lp -d "$PRINTER_NAME" -o raw /tmp/qr-hex.bin

echo "QR code sent! Check printer for output." 