# Panduan Administrator Server

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Kebutuhan Sistem](#kebutuhan-sistem)
3. [Instalasi](#instalasi)
4. [Konfigurasi Sistem](#konfigurasi-sistem)
5. [Konfigurasi Database PostgreSQL](#konfigurasi-database)
6. [Printer Service](#printer-service)
7. [WebSocket Server](#websocket-server)
8. [Manajemen Data Offline](#manajemen-data-offline)
9. [Pemantauan Sistem](#pemantauan-sistem)
10. [Troubleshooting](#troubleshooting)

## Pendahuluan

Panduan ini ditujukan untuk administrator sistem yang bertanggung jawab mengelola infrastruktur server untuk Sistem Parkir Modern. Dokumen ini mencakup aspek teknis dari sistem, termasuk konfigurasi database PostgreSQL, printer thermal, WebSocket server, dan penanganan data offline.

## Kebutuhan Sistem

### Server Requirements
- Windows 10/11 atau Pop!_OS 22.04 LTS
- Minimal 4GB RAM
- 20GB ruang disk
- Koneksi jaringan lokal
- .NET 6.0 Runtime
- PostgreSQL 14 atau lebih baru
- Port COM untuk printer thermal (COM3 default)

### Printer Requirements
- Thermal printer compatible
- Arduino untuk kontrol printer
- Kabel serial
- Driver printer thermal

## Instalasi

### Package Dependencies (Windows)
```powershell
# Install Chocolatey jika belum ada
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

# Install dependencies
choco install dotnet-6.0-runtime -y
choco install postgresql14 -y
```

### Package Dependencies (Pop!_OS)
```bash
# Update sistem
sudo apt update && sudo apt upgrade

# Install PostgreSQL dan dependencies
sudo apt install -y \
    postgresql-14 \
    postgresql-contrib-14 \
    dotnet-sdk-6.0 \
    cups \
    cups-client \
    cups-daemon \
    printer-driver-escpos \
    libcups2 \
    libcupsimage2 \
    nginx \
    certbot \
    ufw
```

### Systemd Service (Linux)
```bash
sudo nano /etc/systemd/system/parkirc.service
```

```ini
[Unit]
Description=ParkIRC Web Application
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/parkirc
ExecStart=/usr/bin/dotnet ParkIRC.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
User=parkirc
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

## Konfigurasi Database

### PostgreSQL Database Setup

1. **Konfigurasi PostgreSQL**:
   ```bash
   # Windows: Run as Administrator
   psql -U postgres
   
   # Linux
   sudo -u postgres psql
   ```

2. **Buat Database dan User**:
   ```sql
   -- Buat database
   CREATE DATABASE parkir2;
   
   -- Verifikasi password untuk user postgres
   ALTER USER postgres WITH PASSWORD 'postgres';
   
   -- Berikan akses ke database
   GRANT ALL PRIVILEGES ON DATABASE parkir2 TO postgres;
   ```

3. **Connection String**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=parkir2;Username=postgres;Password=postgres"
     }
   }
   ```

4. **Backup Database**:
   ```bash
   # Windows
   pg_dump -U postgres -F c parkir2 > "%USERPROFILE%\Documents\Backup\parkir2_%date:~-4%%date:~3,2%%date:~0,2%.backup"

   # Linux
   pg_dump -U postgres -F c parkir2 > ~/backup/parkir2_$(date +%Y%m%d).backup
   ```

5. **Restore Database**:
   ```bash
   # Windows
   pg_restore -U postgres -d parkir2 backup_file.backup

   # Linux
   pg_restore -U postgres -d parkir2 backup_file.backup
   ```

## Printer Service

### Konfigurasi Printer

1. **Port Settings**:
   ```json
   {
     "PrinterSettings": {
       "Port": "COM3",
       "BaudRate": 9600,
       "Timeout": 5000
     }
   }
   ```

2. **Printer Commands**:
   - Initialize: `ESC @ CR LF`
   - Cut Paper: `GS V 1`
   - Bold: `ESC E 1`
   - Normal: `ESC E 0`
   - Center: `ESC a 1`
   - Left Align: `ESC a 0`

3. **Error Handling**:
   - Timeout: 5 seconds
   - Max retries: 3
   - Thread-safe operations dengan SemaphoreSlim

## WebSocket Server

WebSocket server berfungsi sebagai jembatan komunikasi realtime antara aplikasi klien.

### Konfigurasi WebSocket

1. **Port dan Endpoint**:
   - Port default: 8181
   - Endpoint: ws://server_ip:8181/parking

2. **File Konfigurasi WebSocket**:
   - Lokasi: `config/websocket.ini`
   - Parameter konfigurasi:
   ```ini
   [WebSocket]
   Port=8181
   MaxConnections=100
   PingInterval=30
   IdleTimeout=300
   ```

3. **Memulai WebSocket Server**:
   - Otomatis dimulai oleh aplikasi server
   - Untuk memulai manual:
   ```
   ParkingServer.exe --websocket-only
   ```

4. **Protokol Komunikasi**:
   - Format pesan: JSON
   - Event types:
     - `parking_entry`: Kendaraan masuk
     - `parking_exit`: Kendaraan keluar
     - `connection_status`: Status koneksi
     - `sync_request`: Permintaan sinkronisasi

## Manajemen Data Offline

Sistem dirancang untuk menangani operasi offline dan menyinkronkan data saat koneksi tersedia kembali.

### Struktur Penyimpanan Data Offline

1. **Lokasi Data Offline**:
   - Root folder: `offline_data/`
   - Subfolder per aplikasi: `ParkingIN/`, `ParkingOut/`

2. **Format File**:
   - JSON untuk data transaksi
   - Contoh nama file: `parking_entry_20231225_153045_A123BC.json`

3. **Schema Data**:
   ```json
   {
     "transaction_id": "T123456",
     "vehicle_number": "A123BC",
     "entry_time": "2023-12-25T15:30:45",
     "vehicle_type": "CAR",
     "image_path": "Images/Masuk/A123BC_20231225_153045.jpg",
     "sync_status": "PENDING"
   }
   ```

### Proses Sinkronisasi

1. **Algoritma Sinkronisasi**:
   - Prioritas FIFO (First In, First Out)
   - Verifikasi data sebelum insert/update ke database
   - Penanganan konfliks dengan strategi "last-write-wins"

2. **Penanganan Kegagalan**:
   - Retry otomatis dengan backoff eksponensial
   - Maksimal 3 kali percobaan
   - Log detail untuk kegagalan

3. **Pemeliharaan Data**:
   - Data offline yang sudah disinkronkan dipindahkan ke `offline_data/archived/`
   - Data arsip dibersihkan otomatis setelah 30 hari

## Pemantauan Sistem

### Monitoring Terintegrasi

ParkIRC menyediakan monitoring terintegrasi untuk memantau komponen sistem yang penting:

1. **Dasbor Status Sistem**:
   - Akses melalui: Management > System Status
   - Menampilkan status Database, Printer, Kamera
   - Menampilkan penggunaan sistem (disk, memori)
   - Menampilkan log aplikasi dan sistem

2. **Status Indikator**:
   - Tersedia di header aplikasi
   - Hijau: Semua komponen terhubung
   - Merah: Ada masalah koneksi
   - Abu-abu: Status sedang dicek

3. **Notifikasi Real-time**:
   - Pemberitahuan saat terjadi masalah koneksi
   - Pemberitahuan saat layanan dimulai atau dihentikan

### Backup Otomatis

Sistem secara otomatis melakukan backup pada waktu yang dijadwalkan:

1. **Pengaturan Jadwal**:
   - Default: Pukul 03:00 setiap hari
   - Konfigurasi di appsettings.json (`Backup:ScheduledTime`)

2. **Komponen yang di-backup**:
   - Database PostgreSQL
   - Folder uploads (foto kendaraan)
   - File konfigurasi

3. **Rotasi Backup**:
   - Menyimpan backup selama 7 hari (default)
   - Konfigurasi di appsettings.json (`Backup:KeepDays`)

4. **Lokasi Backup**:
   - Folder `/opt/parkirc/backups`
   - Format nama file: `backup_[YYYYMMDD_HHMMSS].zip`

## Troubleshooting

### Masalah Koneksi Database

1. **Koneksi Terputus**:
   - Periksa status server MySQL: `systemctl status mysql`
   - Verifikasi konfigurasi jaringan
   - Periksa kredensial di `config/database.ini`

2. **Koneksi Lambat**:
   - Analisis query dengan `EXPLAIN`
   - Periksa indeks dan optimasi tabel
   - Cek resource server (CPU, memory, disk I/O)

### Masalah WebSocket

1. **Klien Tidak Dapat Terhubung**:
   - Verifikasi port 8181 terbuka di firewall
   - Periksa status WebSocket server di log
   - Coba restart WebSocket server

2. **Pesan Tidak Terkirim**:
   - Periksa koneksi klien
   - Verifikasi format pesan JSON
   - Cek log untuk error parsing

### Masalah Sinkronisasi Data

1. **Data Tidak Tersinkronisasi**:
   - Periksa log di `logs/sync_YYYYMMDD.log`
   - Verifikasi file data offline masih ada
   - Cek permissions pada folder dan file

2. **Data Duplikat**:
   - Identifikasi record duplikat di database
   - Periksa log sinkronisasi untuk anomali
   - Hapus duplikat melalui panel admin

3. **Resolusi Manual**:
   - Gunakan tool `SyncTool.exe` untuk sinkronisasi manual:
   ```
   SyncTool.exe --source offline_data/ParkingIN --force-sync
   ```

## Pemeliharaan Rutin

1. **Backup Database**:
   - Daily full backup
   - Verifikasi integritas backup secara berkala

2. **Pembersihan Data**:
   - Arsip data transaksi > 1 tahun
   - Bersihkan log > 90 hari
   - Hapus gambar kendaraan yang sudah keluar > 30 hari

3. **Update Sistem**:
   - Jadwalkan update pada jam non-peak
   - Selalu backup sebelum update
   - Ikuti prosedur update di `docs/update_procedure.md`

Untuk informasi lebih lanjut atau bantuan, hubungi tim pengembang di developer@parkingsystem.com atau buka ticket support di sistem ticketing internal. 