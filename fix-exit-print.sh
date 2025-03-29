#!/bin/bash
# Exit Gate Printer Diagnostics and Fix Script
# This script diagnoses and fixes common issues with printing from the ExitGate page

echo "===== EXIT GATE PRINTER DIAGNOSTICS ====="
echo "$(date)"
echo

# Define printer name
PRINTER_NAME="TM-T82X-S-A"
echo "Target printer: $PRINTER_NAME"

# Create a test receipt content
echo "Creating test receipt content..."
cat > /tmp/test-receipt.txt << EOF
STRUK PARKIR - KELUAR
===================

No. Tiket    : TEST-$(date +%Y%m%d%H%M%S)
No. Kendaraan: B 1234 XYZ

Waktu Masuk : $(date -d "2 hours ago" +"%d/%m/%Y %H:%M:%S")
Waktu Keluar: $(date +"%d/%m/%Y %H:%M:%S")
Durasi      : 2 jam 0 menit

Total Biaya : Rp 10.000

===================
Terima Kasih


EOF

# Print test file in both methods
echo
echo "===== TESTING PRINT METHODS ====="

# Method 1: Regular printing
echo
echo "Method 1: Standard printing"
lp -d $PRINTER_NAME /tmp/test-receipt.txt
if [ $? -eq 0 ]; then
    echo "✅ Standard print test submitted"
else
    echo "❌ Standard print test failed"
fi

# Method 2: Raw/binary printing
echo
echo "Method 2: Raw/binary printing"
echo "Creating binary file with ESC/POS commands..."

# Create binary print file with ESC/POS commands
{
    # Initialize printer (ESC @)
    printf "\x1B\x40"
    
    # Center align (ESC a 1)
    printf "\x1B\x61\x01"
    
    # Add content from text file
    cat /tmp/test-receipt.txt
    
    # Line feeds
    printf "\n\n\n\n"
    
    # Cut paper (GS V 66 0)
    printf "\x1D\x56\x42\x01"
} > /tmp/test-receipt.bin

# Check binary file
echo "Binary file created: $(ls -la /tmp/test-receipt.bin)"

# Send to printer
lp -d $PRINTER_NAME -o raw /tmp/test-receipt.bin
if [ $? -eq 0 ]; then
    echo "✅ Raw print test submitted"
else
    echo "❌ Raw print test failed"
fi

# Check printer status
echo
echo "===== PRINTER STATUS ====="
lpstat -p $PRINTER_NAME
lpstat -l -p $PRINTER_NAME

# Check printer jobs
echo
echo "===== PRINTER QUEUE ====="
lpq -P $PRINTER_NAME

# Create a system-level diagnostic script for future use
echo
echo "===== CREATING SYSTEM-WIDE DIAGNOSTIC SCRIPT ====="
cat > /usr/local/bin/diagnose-printer.sh << 'EOF'
#!/bin/bash
# Printer diagnostic script
# Usage: diagnose-printer.sh [printer_name]

PRINTER=${1:-"TM-T82X-S-A"}
echo "===== PRINTER DIAGNOSTIC TOOL ====="
echo "Date: $(date)"
echo "Printer: $PRINTER"

# Check if printer exists
echo
echo "1. Checking if printer exists"
lpstat -p | grep $PRINTER
if [ $? -eq 0 ]; then
    echo "✅ Printer exists"
else
    echo "❌ Printer not found"
    echo "Available printers:"
    lpstat -p
fi

# Check printer status
echo
echo "2. Checking printer status"
lpstat -l -p $PRINTER
echo
if lpstat -p $PRINTER | grep -q "idle"; then
    echo "✅ Printer is idle and ready"
else
    echo "⚠️ Printer is not idle"
fi

# Check printer queue
echo
echo "3. Checking printer queue"
lpq -P $PRINTER

# Try direct device access
echo
echo "4. Checking device access"
if [ -e "/dev/lp0" ]; then
    echo "✅ Device /dev/lp0 exists"
    if [ -w "/dev/lp0" ]; then
        echo "✅ Device is writable"
    else
        echo "❌ Device is not writable"
    fi
else
    echo "❌ Device /dev/lp0 does not exist"
fi

# Print test page
echo
echo "5. Attempting to print test page"
{
    printf "\x1B\x40"  # Initialize
    printf "\x1B\x61\x01"  # Center
    echo "TEST PRINT"
    echo "$(date)"
    echo
    echo "Printer: $PRINTER"
    echo
    printf "\x1D\x56\x42\x01"  # Cut
} > /tmp/printer-test.bin

lp -d $PRINTER -o raw /tmp/printer-test.bin
if [ $? -eq 0 ]; then
    echo "✅ Test page sent to printer"
else
    echo "❌ Failed to send test page"
fi

echo
echo "Diagnostic complete"
EOF

# Make the script executable
chmod +x /usr/local/bin/diagnose-printer.sh
echo "✅ Created system-wide diagnostic script: /usr/local/bin/diagnose-printer.sh"

# Check ExitGate controller route
echo
echo "===== CHECKING APPLICATION ROUTES ====="
grep -r "PrintReceipt" --include="*.cs" .
echo

# Fix permissions on printer device (if it exists)
if [ -e "/dev/lp0" ]; then
    echo "===== FIXING PRINTER DEVICE PERMISSIONS ====="
    echo "Device exists: /dev/lp0"
    sudo chmod 666 /dev/lp0
    echo "✅ Updated permissions on /dev/lp0"
    ls -la /dev/lp0
fi

# Make sure CUPS is running
echo
echo "===== CHECKING CUPS SERVICE ====="
systemctl status cups | head -5
if ! systemctl is-active --quiet cups; then
    echo "❌ CUPS service is not running"
    echo "Starting CUPS service..."
    sudo systemctl start cups
else
    echo "✅ CUPS service is running"
fi

echo
echo "===== DIAGNOSTICS COMPLETE ====="
echo "If the test prints were successful, check your printer for output."
echo "If the test prints failed, review the diagnostics above for issues."
echo "Common issues:"
echo "  1. CUPS service not running"
echo "  2. Printer not properly configured"
echo "  3. Permission issues on printer device"
echo "  4. Network connectivity issues (for network printers)"
echo
echo "You can run the diagnostic script again with:"
echo "  sudo /usr/local/bin/diagnose-printer.sh" 