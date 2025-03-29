#!/bin/bash

# Script untuk menguji printer dan koneksinya
PRINTER_NAME="TM-T82X-S-A"

echo "=== TEST PRINTER $PRINTER_NAME ==="
echo ""

# 1. Cek apakah printer ada dalam daftar CUPS
echo "=== 1. Checking printer existence ==="
if lpstat -p "$PRINTER_NAME" > /dev/null 2>&1; then
    echo -e "\033[92m✓ Printer $PRINTER_NAME exists\033[0m"
    lpstat -p "$PRINTER_NAME"
else
    echo -e "\033[91m✗ Printer $PRINTER_NAME not found\033[0m"
    echo "Available printers:"
    lpstat -p
fi

echo ""

# 2. Cek status printer
echo "=== 2. Checking printer status ==="
printer_status=$(lpstat -p "$PRINTER_NAME" | grep "$PRINTER_NAME" | awk '{print $2,$3,$4,$5}')

if [[ "$printer_status" == *"idle"* ]]; then
    echo -e "\033[92m✓ Printer $PRINTER_NAME is idle and ready\033[0m"
else
    echo -e "\033[91m✗ Printer $PRINTER_NAME is not ready: $printer_status\033[0m"
fi

echo ""

# 3. Cek akses ke device printer
echo "=== 3. Checking printer device access ==="
if [ -e "/dev/lp0" ]; then
    if [ -w "/dev/lp0" ]; then
        echo -e "\033[92m✓ Device /dev/lp0 exists and is writable\033[0m"
    else
        echo -e "\033[91m✗ Device /dev/lp0 exists but is not writable\033[0m"
        ls -la /dev/lp0
    fi
else
    echo -e "\033[93m! Device /dev/lp0 not found, CUPS may be using a different device\033[0m"
    echo "Checking all printer devices:"
    ls -la /dev/lp* 2>/dev/null || echo "No /dev/lp* devices found"
    ls -la /dev/usb/lp* 2>/dev/null || echo "No /dev/usb/lp* devices found"
fi

echo ""

# 4. Create test file
echo "=== 4. Creating test print file ==="
cat > /tmp/printer-test.txt << EOF
PRINTER TEST - FULL
===================

This is a test print 
directly from CUPS.

$(date)

===================
EOF

echo -e "\033[92m✓ Test file created at /tmp/printer-test.txt\033[0m"
cat /tmp/printer-test.txt

echo ""

# 5. Send test print
echo "=== 5. Sending test print ==="
echo "Sending plain text print..."
lp_output=$(lp -d "$PRINTER_NAME" /tmp/printer-test.txt 2>&1)
lp_status=$?

if [ $lp_status -eq 0 ]; then
    echo -e "\033[92m✓ Text print job sent successfully\033[0m"
    echo "$lp_output"
else
    echo -e "\033[91m✗ Failed to send text print job\033[0m"
    echo "$lp_output"
fi

echo ""
echo "Sending binary ESC/POS print..."

# Binary print file
cat > /tmp/printer-binary.bin << EOF
$(echo -ne "\x1B\x40")  # Initialize printer
$(echo -ne "\x1B\x61\x01")  # Center alignment
$(echo -ne "\x1B\x21\x00")  # Normal text

BINARY ESC/POS TEST
===================

This is a test print 
using ESC/POS commands.

$(date)

===================
$(echo -ne "\n\n\n\n")  # Line feeds
$(echo -ne "\x1D\x56\x42\x01")  # Partial cut
EOF

lp_output=$(lp -d "$PRINTER_NAME" -o raw /tmp/printer-binary.bin 2>&1)
lp_status=$?

if [ $lp_status -eq 0 ]; then
    echo -e "\033[92m✓ Binary print job sent successfully\033[0m"
    echo "$lp_output"
else
    echo -e "\033[91m✗ Failed to send binary print job\033[0m"
    echo "$lp_output"
fi

echo ""

# 6. Check print queue
echo "=== 6. Checking print queue ==="
lpq_output=$(lpq -P "$PRINTER_NAME")
echo "$lpq_output"

echo ""
echo "=== TEST COMPLETED ==="
echo "If both print jobs were sent successfully, check the printer for output."
echo "The binary ESC/POS print should use center alignment and auto-cut the paper." 