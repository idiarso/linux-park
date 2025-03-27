# Panduan Setup Hardware Printer Thermal

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Spesifikasi Hardware](#spesifikasi-hardware)
3. [Koneksi Hardware](#koneksi-hardware)
4. [Konfigurasi Printer](#konfigurasi-printer)
5. [Testing dan Verifikasi](#testing-dan-verifikasi)
6. [Troubleshooting](#troubleshooting)
7. [Maintenance](#maintenance)

## Pendahuluan

Dokumen ini menjelaskan langkah-langkah setup hardware printer thermal yang digunakan dalam sistem parkir. Printer thermal digunakan untuk mencetak tiket masuk dan keluar, serta struk pembayaran.

## Spesifikasi Hardware

### Printer Thermal
- Model: Compatible dengan ESC/POS command
- Interface: Serial (RS232/TTL)
- Paper width: 58mm atau 80mm
- Print speed: Min. 60mm/detik
- Resolution: 203 DPI
- Power supply: DC 12V/2A

### Arduino Controller
- Model: Arduino Uno R3
- Microcontroller: ATmega328P
- Operating Voltage: 5V
- Digital I/O Pins: 14
- UART: Hardware serial support

### Kabel dan Adaptor
- Kabel serial/TTL
- Power adaptor 12V/2A
- Kabel USB untuk Arduino
- Kabel jumper

## Koneksi Hardware

### Diagram Koneksi
```
Power Supply (12V) --> Printer Thermal
Arduino Uno       --> USB to PC (power & programming)
Arduino TX (1)    --> Printer RX
Arduino RX (0)    --> Printer TX
Arduino GND       --> Printer GND
```

### Langkah-langkah Koneksi
1. Hubungkan power supply ke printer thermal
2. Koneksikan Arduino ke PC menggunakan kabel USB
3. Hubungkan pin TX Arduino ke RX printer
4. Hubungkan pin RX Arduino ke TX printer
5. Hubungkan GND Arduino ke GND printer

## Konfigurasi Printer

### Setting DIP Switch
1. Baud Rate: 9600 (default)
   - SW-1: OFF
   - SW-2: ON
   - SW-3: OFF

2. Print Density:
   - SW-4: ON (High)
   - SW-5: OFF

3. Sensor Type:
   - SW-6: OFF (Gap sensor)

4. Character Set:
   - SW-7: OFF
   - SW-8: ON

### Printer Commands

1. **Initialization**
   ```
   ESC @ - Initialize printer
   ESC ! 0 - Normal text
   ESC ! 1 - Bold text
   ```

2. **Text Formatting**
   ```
   ESC a 0 - Left align
   ESC a 1 - Center align
   ESC a 2 - Right align
   ```

3. **Paper Control**
   ```
   LF - Line feed
   ESC d n - Feed n lines
   GS V 1 - Paper cut
   ```

## Testing dan Verifikasi

### Self-Test
1. Matikan printer
2. Tekan dan tahan tombol FEED
3. Nyalakan printer sambil tetap menahan tombol FEED
4. Lepas tombol setelah printer mulai mencetak

### Test Print via Arduino
1. Upload program test ke Arduino
2. Buka Serial Monitor
3. Kirim perintah test:
   ```
   TEST_PRINT
   ALIGN_CENTER
   PRINT_BOLD
   CUT_PAPER
   ```

### Verifikasi Hasil
- Teks harus terbaca jelas
- Alignment sesuai perintah
- Paper cutting berfungsi
- Tidak ada noise atau artefak

## Troubleshooting

### Common Issues

1. **Printer Tidak Merespon**
   - Periksa power supply
   - Verifikasi koneksi serial
   - Cek setting DIP switch
   - Test self-test print

2. **Kualitas Print Buruk**
   - Bersihkan print head
   - Sesuaikan print density
   - Ganti kertas thermal
   - Cek suhu operasional

3. **Paper Jam**
   - Buka cover printer
   - Keluarkan kertas tersangkut
   - Periksa roller
   - Bersihkan sensor kertas

4. **Komunikasi Error**
   - Verifikasi baudrate
   - Cek koneksi TX/RX
   - Periksa level tegangan
   - Reset Arduino dan printer

### Error Codes
| Code | Description | Solution |
|------|-------------|----------|
| E1 | Paper out | Load new paper roll |
| E2 | Cover open | Close printer cover |
| E3 | High temp | Let printer cool down |
| E4 | Cutter error | Reset printer |
| E5 | Print head error | Contact service |

## Maintenance

### Daily Tasks
- Periksa stok kertas
- Bersihkan bagian luar printer
- Verifikasi kualitas print

### Weekly Tasks
- Bersihkan print head
- Periksa roller
- Test full functionality

### Monthly Tasks
- Backup konfigurasi
- Update firmware jika ada
- Deep cleaning
- Periksa semua koneksi

### Supplies
- Kertas thermal 58mm/80mm
- Cleaning kit
- Spare parts (recommended):
  - Print head
  - Paper roller
  - Power supply 