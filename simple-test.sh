#!/bin/bash

# Simple printer test

PRINTER_NAME="TM-T82X-S-A"
TEST_FILE="/tmp/simple-test.bin"

# Buat file binary dengan perintah ESC/POS sangat sederhana
{
    # ESC @ - Reset printer
    printf "\x1B\x40"
    
    # Double height and width
    printf "\x1B\x21\x30"
    printf "TEST PRINTER\n\n"
    
    # Normal font
    printf "\x1B\x21\x00"
    printf "SIMPLE TEST\n\n\n"
    
    # Cut paper
    printf "\x1D\x56\x01"
    
} > "$TEST_FILE"

# Cetak file dengan perintah ESC/POS
echo "Mengirim tes sederhana ke printer $PRINTER_NAME..."
echo "File: $TEST_FILE"
cat "$TEST_FILE" | lp -d "$PRINTER_NAME" -o raw

# Alternatif langsung
echo -e "\033@\033!\060\nTEST LANGSUNG\n\033!\000\nALTERNATIF\n\n\035V\001" | lp -d "$PRINTER_NAME" -o raw 