#!/bin/bash

# Script untuk mencetak barcode sebagai ASCII text ke printer thermal
# Script ini menggunakan cara alternatif ketika metode gambar tidak berhasil

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
BARCODE_CONTENT="$1"

if [ -z "$BARCODE_CONTENT" ]; then
    echo "Error: Isi barcode tidak diberikan"
    echo "Penggunaan: $0 <barcode_content>"
    exit 1
fi

echo "Mencetak barcode: $BARCODE_CONTENT"

# Buat file tiket
TEMP_FILE=$(mktemp)

# Gunakan karakter ASCII untuk membuat tiket
cat > "$TEMP_FILE" << EOF

============================
   PARKIR - TEST BARCODE
============================

Barcode: $BARCODE_CONTENT
Waktu  : $(date +"%d-%m-%Y %H:%M:%S")

-----------------------------
*$BARCODE_CONTENT*
-----------------------------

    TERIMA KASIH
    
    
    
    
EOF

# Cetak tiket
echo "Mengirim ke printer $PRINTER_NAME..."
lp -d "$PRINTER_NAME" "$TEMP_FILE"

# Bersihkan
rm -f "$TEMP_FILE"
echo "Selesai" 