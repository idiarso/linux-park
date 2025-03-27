# ParkIRC - Sistem Manajemen Parkir Modern

ParkIRC adalah sistem manajemen parkir modern yang menggabungkan otomatisasi di pintu masuk dan verifikasi operator di pintu keluar untuk pengalaman parkir yang efisien dan aman.

## Fitur Utama

### 1. Pintu Masuk (Full Otomatis)
- Operasi dipicu oleh push button yang ditekan pengguna
- Deteksi otomatis kendaraan dan plat nomor menggunakan kamera
- Cetak tiket dengan barcode secara otomatis
- Gate terbuka otomatis setelah tiket diambil
- Pencatatan otomatis data masuk ke database (waktu, foto, plat nomor)

### 2. Pintu Keluar (Operator)
- Scan barcode tiket oleh operator
- Perhitungan biaya otomatis berdasarkan durasi
- Verifikasi pembayaran oleh operator
- Cetak struk pembayaran
- Gate terbuka setelah pembayaran selesai

### 3. Monitoring Real-time
- Status pintu masuk dan keluar
- Status perangkat (kamera, printer, push button)
- Riwayat transaksi terkini
- Riwayat kendaraan dengan filter:
  - Status (Masuk/Keluar)
  - Rentang waktu
  - Jenis kendaraan
  - Plat nomor
- Notifikasi kejadian penting
- Monitoring jumlah kendaraan

### 4. Manajemen
- Kelola slot parkir
- Kelola operator dan shift
- Konfigurasi tarif parkir
- Pengaturan kamera dan printer
- Riwayat lengkap kendaraan:
  - Pencarian berdasarkan status masuk/keluar
  - Filter berdasarkan tanggal
  - Export data ke Excel/PDF
  - Detail transaksi per kendaraan
- Laporan dan statistik

## Teknologi

- **Backend**: ASP.NET Core 6.0
- **Frontend**: Bootstrap 5, jQuery
- **Database**: PostgreSQL
- **Real-time**: SignalR
- **Hardware Integration**:
  - Kamera IP untuk ANPR (Automatic Number Plate Recognition)
  - Thermal printer untuk tiket dan struk
  - Push button untuk trigger pintu masuk
  - Gate controller untuk palang pintu
  - Barcode scanner di pintu keluar

## Persyaratan Sistem

- .NET 6.0 SDK
- PostgreSQL 13+
- Nginx/Apache (untuk production)
- Modern web browser
- Perangkat keras yang kompatibel

## Instalasi

1. Clone repository
```bash
git clone https://github.com/idiarsopgl/parkir28Mar.git
cd parkir28Mar
```

2. Restore dependencies
```bash
dotnet restore
```

3. Update database (pastikan PostgreSQL sudah terpasang dan berjalan)
```bash
# Jika PostgreSQL belum terpasang, install terlebih dahulu:
# Windows: 
# 1. Download PostgreSQL dari https://www.postgresql.org/download/windows/
# 2. Ikuti wizard instalasi, catat username dan password
# 3. Buat database baru untuk aplikasi

# Linux:
# sudo apt update
# sudo apt install postgresql postgresql-contrib
# sudo -u postgres createdb parkirc_db

# Setelah PostgreSQL terpasang, jalankan:
dotnet ef database update
```

4. Setup konfigurasi
- Buka file `appsettings.json` dan sesuaikan:
  - Koneksi database PostgreSQL
  - Pengaturan kamera dan printer
  - Konfigurasi email untuk notifikasi
  - Pengaturan SignalR dan caching

5. Jalankan aplikasi
```bash
dotnet run
```

6. Akses aplikasi
```
http://localhost:5126
```

## Konfigurasi Hardware

### Pintu Masuk
1. Setup kamera IP
   - Konfigurasi di menu Settings > Devices > Cameras
   - Pastikan posisi optimal untuk ANPR
   
2. Koneksi push button
   - Hubungkan mikrokontroler sesuai dengan diagram yang disediakan
   - Test koneksi menggunakan simulator di menu Diagnostics

3. Setup printer tiket
   - Konfigurasi di menu Settings > Devices > Printers
   - Test cetak tiket dari menu Entry Gate

### Pintu Keluar
1. Koneksi barcode scanner
   - Konfigurasi sebagai keyboard emulation
   - Test pembacaan tiket di menu Exit Gate

2. Setup printer struk
   - Konfigurasi di menu Settings > Devices > Receipt Printers
   - Test cetak struk dari menu Exit Gate

## Pemecahan Masalah

- **Koneksi Database**: Pastikan PostgreSQL berjalan dan kredensial pada appsettings.json benar
- **Kamera Tidak Berfungsi**: Periksa alamat IP, penempatan, dan pencahayaan
- **Printer Error**: Periksa koneksi printer, kertas, dan driver
- **Gate Tidak Berfungsi**: Verifikasi koneksi hardware dan konfigurasi pin

## Kontribusi

1. Fork repository
2. Buat branch fitur (`git checkout -b feature/NamaFitur`)
3. Commit perubahan (`git commit -m 'Menambahkan fitur baru'`)
4. Push ke branch (`git push origin feature/NamaFitur`)
5. Buat Pull Request

## Lisensi

Sistem ini didistribusikan di bawah Lisensi MIT. Lihat file `LICENSE` untuk informasi lebih lanjut.

## Kontak

ParkIRC Team - support@parkirc.com

Project Link: [https://github.com/idiarsopgl/parkir28Mar](https://github.com/idiarsopgl/parkir28Mar)
