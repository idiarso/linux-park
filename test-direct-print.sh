#!/bin/bash

# Script untuk menguji printer langsung
PRINTER_NAME="TM-T82X-S-A"

echo "Menguji printer $PRINTER_NAME..."

# Buat file temporary untuk data ESC/POS
cat > /tmp/direct-print.bin << EOF
$(echo -ne "\x1B\x40")  # Initialize printer
$(echo -ne "\x1B\x61\x01")  # Center alignment
$(echo -ne "\x1B\x21\x00")  # Normal text

TEST PRINTER LANGSUNG
===================

Ini adalah test cetak langsung
melalui ESC/POS commands.
Tanpa melalui aplikasi.

$(date)

===================
BERHASIL JIKA TERCETAK
===================

$(echo -ne "\n\n\n\n")  # Line feeds
$(echo -ne "\x1D\x56\x42\x01")  # Partial cut
EOF

# Kirim ke printer
lp -d "$PRINTER_NAME" -o raw /tmp/direct-print.bin

echo "Perintah cetak dikirim ke $PRINTER_NAME."
echo "Cek printer untuk hasilnya." 