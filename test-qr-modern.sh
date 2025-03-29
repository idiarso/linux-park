#!/bin/bash

# QR Code Test Script - Modern Epson
# Untuk printer Epson TM series terbaru

PRINTER_NAME="TM-T82X-S-A"
PORT_NAME="/dev/lp0"

# Coba gunakan direct atau CUPS
if [ "$1" = "direct" ]; then
    USE_DIRECT=true
    echo "Menggunakan direct printing ke port $PORT_NAME"
else
    USE_DIRECT=false
    echo "Menggunakan CUPS printing ke printer $PRINTER_NAME"
fi

# Tulis fixed binary ESC/POS command untuk QR code
cat > /tmp/qr-modern.bin << EOF
$(echo -en "\x1B\x40")
$(echo -en "\x1B\x61\x01")
$(echo -en "\n\nQR CODE TEST - MODERN\n\n")

$(echo -en "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00")
$(echo -en "\x1D\x28\x6B\x03\x00\x31\x43\x05")
$(echo -en "\x1D\x28\x6B\x03\x00\x31\x45\x31")
$(echo -en "\x1D\x28\x6B\x0B\x00\x31\x50\x30\x31\x32\x33\x34\x35\x36\x37")
$(echo -en "\x1D\x28\x6B\x03\x00\x31\x51\x30")

$(echo -en "\n\n\nALTERNATIF 2:\n\n")

$(echo -en "\x1B\x29\x68\x32") 
$(echo -en "\x1B\x29\x68\x33\x30")
$(echo -en "\x1B\x29\x68\x43\x35")
$(echo -en "\x1B\x29\x6B\x31TEST123")
$(echo -en "\x1B\x29\x6B\x50")

$(echo -en "\n\n\n\n")
$(echo -en "\x1D\x56\x01")
EOF

# Periksa binary file
echo "Binary file created at /tmp/qr-modern.bin"
echo "Checking format..."
hexdump -C /tmp/qr-modern.bin | head -20

# Kirim ke printer
if [ "$USE_DIRECT" = true ]; then
    if [ -e "$PORT_NAME" ]; then
        echo "Sending directly to $PORT_NAME..."
        cat /tmp/qr-modern.bin > "$PORT_NAME"
    else
        echo "Error: Port $PORT_NAME not found"
        echo "Trying with CUPS instead..."
        lp -d "$PRINTER_NAME" -o raw /tmp/qr-modern.bin
    fi
else
    echo "Sending to printer $PRINTER_NAME via CUPS..."
    lp -d "$PRINTER_NAME" -o raw /tmp/qr-modern.bin
fi

echo "QR code sent to printer!" 