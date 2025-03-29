#!/bin/bash

# Nama printer
PRINTER_NAME="TM-T82X-S-A"

echo "Testing direct binary ESC/POS print to $PRINTER_NAME..."

# Membuat file binary dengan ESC/POS commands 
# Hapus file sebelumnya jika ada
rm -f /tmp/direct-print.bin

# Membuat file binary baru
# Initialize printer (ESC @)
echo -ne "\x1B\x40" > /tmp/direct-print.bin

# Set centering (ESC a 1)
echo -ne "\x1B\x61\x01" >> /tmp/direct-print.bin

# Set bold (ESC E 1)
echo -ne "\x1B\x45\x01" >> /tmp/direct-print.bin

# Header
echo -ne "\nTEST PRINTER BINARY\n" >> /tmp/direct-print.bin
echo -ne "===================\n\n" >> /tmp/direct-print.bin

# Reset bold (ESC E 0)
echo -ne "\x1B\x45\x00" >> /tmp/direct-print.bin

# Content
echo -ne "Ini adalah test cetak langsung\n" >> /tmp/direct-print.bin
echo -ne "melalui binary ESC/POS.\n" >> /tmp/direct-print.bin
echo -ne "Tanpa melalui PrintService.\n\n" >> /tmp/direct-print.bin
echo -ne "Waktu: $(date)\n\n" >> /tmp/direct-print.bin

# Set bold (ESC E 1)
echo -ne "\x1B\x45\x01" >> /tmp/direct-print.bin
echo -ne "===================\n" >> /tmp/direct-print.bin
echo -ne "BERHASIL JIKA TERCETAK\n" >> /tmp/direct-print.bin
echo -ne "===================\n\n\n\n" >> /tmp/direct-print.bin

# Reset (ESC @)
echo -ne "\x1B\x40" >> /tmp/direct-print.bin

# Cut paper (GS V 66 1)
echo -ne "\x1D\x56\x42\x01" >> /tmp/direct-print.bin

# Print to device
echo "Mengirim data ke printer..."
cat /tmp/direct-print.bin | hexdump -C | head -10
echo "..."

# Send to printer
lp -d "$PRINTER_NAME" -o raw /tmp/direct-print.bin

echo "Perintah cetak dikirim ke $PRINTER_NAME. Cek printer untuk hasil." 