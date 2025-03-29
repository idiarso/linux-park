#!/bin/bash

# Tes Dasar untuk Thermal Printer
# Script ini menggunakan format paling dasar yang harus bekerja
# pada hampir semua printer ESC/POS

# Pilih metode cetak
MODE=$1
if [ -z "$MODE" ]; then
    echo "Gunakan: $0 [cups|direct|all]"
    echo "  cups   - Cetak menggunakan CUPS (lp command)"
    echo "  direct - Cetak langsung ke port printer (perlu izin)"
    echo "  all    - Coba kedua metode"
    exit 1
fi

# Buat file ESC/POS sederhana
TEMP_FILE="/tmp/test-thermal.bin"

# Hanya perintah paling dasar untuk reset dan text
{
    # ESC @ - Reset printer
    echo -ne "\x1B\x40"
    
    # Teks biasa
    echo -e "\n\nTEST THERMAL PRINTER\n\n"
    
    # Atur font jadi besar
    echo -ne "\x1B\x21\x30"
    echo -e "PRINTER TEST\n\n"
    
    # Reset font ke normal
    echo -ne "\x1B\x21\x00"
    echo -e "If you can see this\nPrinter is working\n\n\n"
    
} > "$TEMP_FILE"

# Print dengan CUPS
if [ "$MODE" = "cups" ] || [ "$MODE" = "all" ]; then
    echo "===== Mencoba cetak via CUPS ====="
    lp -d TM-T82X-S-A -o raw "$TEMP_FILE"
    echo "Request cetak terkirim ke CUPS"
fi

# Print langsung ke port printer
if [ "$MODE" = "direct" ] || [ "$MODE" = "all" ]; then
    echo "===== Mencoba cetak langsung ke port ====="
    if [ -e "/dev/lp0" ]; then
        cat "$TEMP_FILE" > /dev/lp0 && echo "Data terkirim ke /dev/lp0"
    elif [ -e "/dev/usb/lp0" ]; then
        cat "$TEMP_FILE" > /dev/usb/lp0 && echo "Data terkirim ke /dev/usb/lp0"
    else
        echo "Tidak menemukan port printer (/dev/lp0 atau /dev/usb/lp0)"
        echo "Coba jalankan: sudo chmod 666 /dev/lp0"
    fi
fi

echo "Selesai! Cek printer untuk hasil." 