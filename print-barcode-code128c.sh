#!/bin/bash
# Script untuk mencetak barcode CODE-128 Code C (khusus angka) menggunakan ESC/POS commands

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
BARCODE_TEXT="$1"

if [ -z "$BARCODE_TEXT" ]; then
    echo "Error: Isi barcode tidak diberikan"
    echo "Penggunaan: $0 <barcode_content>"
    exit 1
fi

# Cek apakah hanya angka
if [[ ! $BARCODE_TEXT =~ ^[0-9]+$ ]]; then
    echo "Error: Untuk CODE-128C, data harus berupa angka saja"
    exit 1
fi

# Cek apakah jumlah digit genap (CODE-128C memerlukan jumlah genap)
if [ $((${#BARCODE_TEXT} % 2)) -ne 0 ]; then
    # Tambahkan 0 di depan jika ganjil
    BARCODE_TEXT="0$BARCODE_TEXT"
    echo "Menambahkan 0 di depan untuk memastikan jumlah digit genap: $BARCODE_TEXT"
fi

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# File untuk perintah ESC/POS
ESCPOS_FILE="$TEMP_DIR/barcode_code128c.bin"

echo "Data Barcode: $BARCODE_TEXT (${#BARCODE_TEXT} karakter)"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n"
    
    # ----- Teks header -----
    printf "BARCODE CODE-128 (MODE C)\n\n"
    printf "Data: %s\n\n" "$BARCODE_TEXT"
    
    # ----- Barcode Format -----
    # GS h - Set barcode height - 120 dots
    printf "\x1D\x68\x78"
    
    # GS w - Set barcode width (2-6) - width 4 (lebih tebal)
    printf "\x1D\x77\x04"
    
    # GS H - Set barcode text position (0=none, 1=above, 2=below, 3=both)
    printf "\x1D\x48\x02"
    
    # GS f - HRI Font (0=A, 1=B)
    printf "\x1D\x66\x00"
    
    # GS k - Print barcode (code 73 for CODE128)
    printf "\x1D\x6B\x49"
    
    # Barcode data length
    # +1 karena kita tambahkan satu byte untuk Start C
    printf "\\$(printf '%03o' "$((${#BARCODE_TEXT} + 1))")"
    
    # Start C (ASCII 205 / Hex CD)
    printf "\xCD"
    
    # Barcode data
    printf "%s" "$BARCODE_TEXT"
    
    # Line feed untuk jarak
    printf "\n\n"
    
    # ----- Footer -----
    printf "======================\n\n"
    
    # Cut paper
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah ESC/POS untuk barcode CODE-128C ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Selesai" 