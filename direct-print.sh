#!/bin/bash

# Metode direct printing ke port printer
echo "Mencoba metode direct printing ke port printer..."

# Cari port printer yang mungkin
for PORT in /dev/usb/lp* /dev/lp*
do
    if [ -e "$PORT" ]; then
        echo "Mencoba port: $PORT"
        echo -e "\x1B\x40\x1B\x21\x30TEST DIRECT\x1B\x21\x00\n\n" > "$PORT" 2>/dev/null
        
        if [ $? -eq 0 ]; then
            echo "Berhasil menulis ke $PORT"
        else
            echo "Gagal menulis ke $PORT (mungkin perlu izin sudo)"
        fi
    fi
done

# Coba dengan sudo jika perlu
echo -e "\nJika tidak ada port yang berfungsi, coba jalankan dengan sudo:"
echo "sudo bash $0" 