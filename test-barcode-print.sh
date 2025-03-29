#!/bin/bash

# Matikan aplikasi yang berjalan
pkill -f dotnet

# Buat ticket barcode sederhana
TICKET="TEST-$(date '+%Y%m%d%H%M%S')"
BARCODE_DIR="./images/barcodes"
mkdir -p $BARCODE_DIR

# Gunakan command lp untuk print test
echo -e "\n\n===== TEST BARCODE PRINTER =====\n" > test-print.txt
echo -e "Ticket: $TICKET" >> test-print.txt
echo -e "Date: $(date '+%Y-%m-%d %H:%M:%S')" >> test-print.txt
echo -e "\n\n\n\n\n\n\n" >> test-print.txt

# Print dengan lp command
echo "Printing test ticket..."
lp -d TM-T82X-S-A test-print.txt

# Clean up
rm test-print.txt

echo "Test complete!" 