# Panduan Setup Mikrokontroler AVR untuk Printer Thermal

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Kebutuhan Hardware](#kebutuhan-hardware)
3. [Koneksi Hardware](#koneksi-hardware)
4. [Setup Arduino IDE](#setup-arduino-ide)
5. [Upload Program](#upload-program)
6. [Troubleshooting](#troubleshooting)

## Pendahuluan

Dokumen ini menjelaskan langkah-langkah setup mikrokontroler Arduino untuk mengendalikan printer thermal dalam sistem parkir. Mikrokontroler berfungsi sebagai jembatan antara aplikasi utama dengan printer thermal.

## Kebutuhan Hardware

1. **Arduino Board**:
   - Arduino Uno R3 atau yang kompatibel
   - Kabel USB untuk programming
   - Power supply 12V (jika diperlukan)

2. **Printer Thermal**:
   - Printer thermal compatible dengan ESC/POS commands
   - Kabel serial/TTL
   - Power supply printer

3. **Komponen Tambahan**:
   - Kabel jumper
   - MAX232 (jika diperlukan untuk konversi level TTL)
   - PCB atau breadboard untuk prototyping

## Koneksi Hardware

### Pin Connections
```
Arduino     Printer Thermal
TX (1)  ->  RX
RX (0)  ->  TX
GND     ->  GND
```

### Menggunakan MAX232 (Opsional)
```
Arduino     MAX232      Printer
TX    ->    T2IN   ->  RX
RX    ->    R2OUT  ->  TX
GND   ->    GND    ->  GND
```

## Setup Arduino IDE

1. **Install Software**:
   - Download Arduino IDE dari [arduino.cc](https://www.arduino.cc/)
   - Install driver CH340 jika menggunakan clone Arduino

2. **Konfigurasi IDE**:
   - Board: Arduino Uno
   - Processor: ATmega328P
   - Port: COM3 (atau sesuai yang terdeteksi)
   - Baudrate: 9600

## Upload Program

### Basic Printer Control Code
```cpp
#include <SoftwareSerial.h>

// Pin definitions
#define PRINTER_RX 2
#define PRINTER_TX 3

// Initialize printer serial
SoftwareSerial printer(PRINTER_RX, PRINTER_TX);

void setup() {
  // Start serial communications
  Serial.begin(9600);
  printer.begin(9600);
  
  // Initialize printer
  printer.write(27);
  printer.write(64);
}

void loop() {
  // Check for commands from main application
  if (Serial.available()) {
    // Forward data to printer
    printer.write(Serial.read());
  }
  
  // Check for responses from printer
  if (printer.available()) {
    Serial.write(printer.read());
  }
}
```

### Command Protocol
1. **Format Perintah**:
   ```
   <START><COMMAND><LENGTH><DATA><END>
   ```
   - START: 0x02
   - COMMAND: 1 byte command
   - LENGTH: 1 byte data length
   - DATA: Variable length
   - END: 0x03

2. **Daftar Command**:
   - 0x10: Print text
   - 0x11: Cut paper
   - 0x12: Check status
   - 0x13: Feed paper
   - 0x14: Reset printer

## Troubleshooting

### Common Issues

1. **Printer Tidak Merespon**:
   - Periksa koneksi kabel
   - Verifikasi baudrate
   - Cek power supply
   - Test dengan command sederhana

2. **Karakter Tidak Terbaca**:
   - Verifikasi baudrate setting
   - Cek level tegangan TTL
   - Periksa ground connection

3. **Printer Error**:
   - Cek status paper
   - Verifikasi format command
   - Reset printer dan Arduino

### Debug Tips
1. Gunakan Serial Monitor untuk debug
2. Tambahkan LED indicator untuk status
3. Implementasi error logging
4. Test dengan command dasar dulu

### Maintenance
1. Periksa koneksi secara berkala
2. Bersihkan printer head
3. Update firmware jika ada
4. Backup konfigurasi 