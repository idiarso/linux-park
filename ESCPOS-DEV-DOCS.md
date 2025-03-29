# ESC/POS Technical Documentation

Dokumen ini berisi referensi teknis untuk ESC/POS commands yang digunakan dalam script barcode dan QR code.

## Daftar Isi

1. [Pengenalan ESC/POS](#pengenalan-escpos)
2. [Format Dasar Command](#format-dasar-command)
3. [Printer Control Commands](#printer-control-commands)
4. [Barcode Commands](#barcode-commands)
5. [QR Code Commands](#qr-code-commands)
6. [Troubleshooting](#troubleshooting)

## Pengenalan ESC/POS

ESC/POS (ESC/Point of Sale) adalah bahasa perintah proprietari yang dibuat oleh Epson untuk printer thermal dan dot-matrix. Ini memungkinkan kontrol rinci atas printer termasuk pengaturan font, mode cetak, dan mencetak barcode/QR code.

## Format Dasar Command

ESC/POS commands terdiri dari:
- **Control Character**: Biasanya dimulai dengan escape character (`0x1B`, `\x1B`) atau GS character (`0x1D`, `\x1D`)
- **Command Character**: Menentukan jenis perintah
- **Parameters**: Nilai yang mengontrol perilaku perintah

Contoh:
```
ESC @ = \x1B\x40 (Reset printer)
GS h n = \x1D\x68\x50 (Set barcode height to 80 dots)
```

## Printer Control Commands

### Reset Printer
```
ESC @
Hex: 1B 40
```
Menginisialisasi printer, menghapus buffer dan mengatur printer ke pengaturan default.

### Font & Text Formatting

#### Select Font
```
ESC M n
Hex: 1B 4D n
```
`n = 0`: Font A (default)
`n = 1`: Font B (lebih kecil)

#### Set Character Size
```
ESC !
Hex: 1B 21 n
```
`n`: Bitmask yang mengontrol ukuran karakter dan styling

#### Text Alignment
```
ESC a n
Hex: 1B 61 n
```
`n = 0`: Kiri
`n = 1`: Tengah
`n = 2`: Kanan

## Barcode Commands

### Barcode Height
```
GS h n
Hex: 1D 68 n
```
`n`: Tinggi barcode dalam dots (1-255)

### Barcode Width
```
GS w n
Hex: 1D 77 n
```
`n`: Lebar barcode (2-6, default = 3)

### HRI (Human Readable Interpretation) Position
```
GS H n
Hex: 1D 48 n
```
`n = 0`: Tidak tampilkan text
`n = 1`: Tampilkan di atas barcode
`n = 2`: Tampilkan di bawah barcode
`n = 3`: Tampilkan di atas dan bawah barcode

### Print Barcode
```
GS k m n data
Hex: 1D 6B m n ...data...
```
Untuk CODE128:
```
GS k 73 n data
Hex: 1D 6B 49 n ...data...
```
Dimana `n` adalah panjang data dan `data` adalah isi barcode.

## QR Code Commands

ESC/POS memiliki beberapa tahap untuk mencetak QR code:

### 1. Set QR Code Model
```
GS ( k pL pH cn fn n
Hex: 1D 28 6B 04 00 31 41 n m
```
Dimana:
- `pL, pH`: Panjang parameter (4, 0)
- `cn`: 49 ('1')
- `fn`: 65 ('A')
- `n`: Model (1,2)
- `m`: 0

### 2. Set QR Code Size
```
GS ( k pL pH cn fn n
Hex: 1D 28 6B 03 00 31 43 n
```
Dimana:
- `n`: Ukuran dot (1-16)

### 3. Set Error Correction Level
```
GS ( k pL pH cn fn n
Hex: 1D 28 6B 03 00 31 45 n
```
Dimana:
- `n = 48`: Level L (7%)
- `n = 49`: Level M (15%)
- `n = 50`: Level Q (25%)
- `n = 51`: Level H (30%)

### 4. Store QR Code Data
```
GS ( k pL pH cn fn m data
Hex: 1D 28 6B pL pH 31 50 30 ...data...
```
Dimana:
- `pL, pH`: Panjang parameter (len(data) + 3, dalam 2 byte format little-endian)
- `cn`: 49 ('1')
- `fn`: 80 ('P')
- `m`: 48 ('0')
- `data`: Data untuk QR code

### 5. Print QR Code
```
GS ( k pL pH cn fn m
Hex: 1D 28 6B 03 00 31 51 30
```

## Troubleshooting

### Barcode Tidak Dicetak

1. **Periksa panjang barcode**: Untuk CODE128, pastikan panjang data tidak melebihi batas maksimum
2. **Kontrol karakter**: Pastikan data tidak berisi kontrol karakter yang dapat mempengaruhi printer
3. **Format**: Pastikan urutan command dan parameter benar

### QR Code Tidak Muncul

1. **Ukuran buffer**: Perhatikan ukuran data QR code. Jika terlalu besar, printer mungkin tidak dapat memprosesnya
2. **Parameter pL & pH**: Hitung dengan benar pL dan pH (len+3)
3. **Ukuran module**: Jika ukuran terlalu kecil, QR code mungkin tidak terbaca

### Command Tidak Bekerja

1. **Kompatibilitas**: Tidak semua printer mendukung semua perintah ESC/POS
2. **Implementasi berbeda**: Beberapa printer mungkin memiliki variasi dalam implementasi command
3. **Pengaturan printer**: Beberapa printer mungkin memerlukan pengaturan khusus sebelum menerima perintah ESC/POS

### Penggunaan Raw Mode

Gunakan mode raw saat mengirim perintah ESC/POS:
```bash
lp -d PRINTER_NAME -o raw FILE
```

## Referensi

- [Epson ESC/POS Command Reference](https://reference.epson-biz.com/modules/ref_escpos/index.php)
- [ESC/POS Commands Cheatsheet](https://www.novopos.ch/client/EPSON/ESC-POS_Command_Cheat_Sheet.pdf) 