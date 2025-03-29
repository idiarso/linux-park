#!/bin/bash

# Script untuk mencetak tiket parkir lengkap dengan QR code
# Dirancang untuk printer Epson TM-T82X menggunakan ESC/POS commands

# Ambil parameter
PRINTER_NAME="TM-T82X-S-A"
TICKET_NUMBER="${1:-AUTO-$(date +%Y%m%d%H%M%S)}"
VEHICLE_NUMBER="${2:-}"
ENTRY_TIME="$(date '+%d/%m/%Y %H:%M:%S')"
LOCATION="${3:-Gate 1}"

# Buat direktori temp jika belum ada
TEMP_DIR="/tmp/barcode-print"
mkdir -p "$TEMP_DIR"

# File untuk perintah ESC/POS
ESCPOS_FILE="$TEMP_DIR/ticket_print.bin"

# Generate QR code content - bisa berupa URL atau data tiket
QR_CONTENT="TICKET:$TICKET_NUMBER;ENTRY:$ENTRY_TIME"
if [ ! -z "$VEHICLE_NUMBER" ]; then
    QR_CONTENT="$QR_CONTENT;VEHICLE:$VEHICLE_NUMBER"
fi
QR_SIZE=8

# Fungsi untuk menangani panjang data QR code
function qr_data_command() {
    local data="$1"
    local data_len=${#data}
    
    # QR code data command header: GS ( k pL pH cn fn m data
    printf "\x1D\x28\x6B" # GS ( k
    
    # Hitung pL dan pH berdasarkan panjang data
    # pL = (data_len + 3) % 256
    # pH = (data_len + 3) / 256
    local command_size=$((data_len + 3))
    local pL=$((command_size & 0xff))
    local pH=$((command_size >> 8))
    
    # Output pL dan pH sebagai bytes
    printf "\\$(printf '%03o' $pL)"
    printf "\\$(printf '%03o' $pH)"
    
    # cn fn m: 49 80 48 (31 50 30 dalam hex) = 1 P 0
    printf "\x31\x50\x30"
    
    # Tambahkan data QR code
    printf "%s" "$data"
}

# Tampilkan informasi tiket
echo "Mencetak tiket dengan informasi:"
echo "Nomor Tiket: $TICKET_NUMBER"
[ ! -z "$VEHICLE_NUMBER" ] && echo "Nomor Kendaraan: $VEHICLE_NUMBER"
echo "Waktu Masuk: $ENTRY_TIME"
echo "Lokasi: $LOCATION"
echo "QR Content: $QR_CONTENT"

# Buat file binary dengan perintah ESC/POS
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Center alignment
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n"
    
    # ----- Header Tiket -----
    # Double height and width
    printf "\x1B\x21\x30"
    printf "PARK IRC\n"
    
    # Normal font
    printf "\x1B\x21\x00"
    printf "PARKING SYSTEM\n"
    printf "==============================\n\n"
    
    # Emphasized
    printf "\x1B\x21\x08"
    printf "TIKET PARKIR\n"
    printf "ENTRY TICKET\n\n"
    
    # Normal font
    printf "\x1B\x21\x00"
    
    # Left alignment for details
    printf "\x1B\x61\x00"
    
    # ----- Informasi Tiket -----
    printf "Nomor Tiket : %s\n" "$TICKET_NUMBER"
    printf "Tanggal     : %s\n" "$(echo $ENTRY_TIME | cut -d' ' -f1)"
    printf "Jam Masuk   : %s\n" "$(echo $ENTRY_TIME | cut -d' ' -f2)"
    
    # Print vehicle info if available
    if [ ! -z "$VEHICLE_NUMBER" ]; then
        printf "No. Kendaraan: %s\n" "$VEHICLE_NUMBER"
    fi
    
    # Location info
    printf "Lokasi      : %s\n\n" "$LOCATION"
    
    # Center alignment for QR code
    printf "\x1B\x61\x01"
    
    # ----- QR Code Generation -----
    # Set QR Code Model (model 2)
    printf "\x1D\x28\x6B\x04\x00\x31\x41\x32\x00"
    
    # Set QR Code Size (1-16)
    printf "\x1D\x28\x6B\x03\x00\x31\x43"
    printf "\\$(printf '%03o' $QR_SIZE)"
    
    # Set error correction level (L=0, M=1, Q=2, H=3)
    printf "\x1D\x28\x6B\x03\x00\x31\x45\x33"  # Level H (highest)
    
    # Store QR Code data
    qr_data_command "$QR_CONTENT"
    
    # Print QR Code
    printf "\x1D\x28\x6B\x03\x00\x31\x51\x30"
    
    # Center alignment for footer
    printf "\x1B\x61\x01"
    
    # Line feed untuk jarak
    printf "\n"
    
    # ----- Footer -----
    printf "==============================\n"
    printf "Terima Kasih\n"
    printf "Simpan Tiket Ini\n"
    printf "Kehilangan Tiket Dikenakan Denda\n\n"
    
    # Cut paper
    printf "\x1D\x56\x01"
    
} > "$ESCPOS_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim perintah untuk mencetak tiket ke printer..."
lp -d "$PRINTER_NAME" -o raw "$ESCPOS_FILE"

# Untuk debug
echo "File ESC/POS tersimpan di: $ESCPOS_FILE"
echo "Tiket berhasil dicetak!" 