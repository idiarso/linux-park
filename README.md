# ParkIRC - Sistem Manajemen Parkir Modern

ParkIRC adalah sistem manajemen parkir modern yang menggabungkan otomatisasi di pintu masuk dan verifikasi operator di pintu keluar untuk pengalaman parkir yang efisien dan aman.

## Fitur Utama

### 1. Pintu Masuk (Full Otomatis)
- Operasi dipicu oleh push button yang ditekan pengguna
- Deteksi otomatis kendaraan dan plat nomor menggunakan kamera
  - Mendukung kamera USB (DirectShow) dan IP Camera
  - Pengaturan resolusi dan frame rate yang dapat dikonfigurasi
  - Penyimpanan gambar otomatis dengan format dan lokasi yang dapat disesuaikan
- Cetak tiket dengan barcode secara otomatis
  - Format tiket yang dapat dikustomisasi
  - Mendukung berbagai jenis printer thermal
  - Barcode format Code128 untuk keamanan
- Gate terbuka otomatis setelah tiket diambil
  - Kontrol gate melalui serial port atau GPIO
  - Timeout yang dapat dikonfigurasi
  - Deteksi status gate (terbuka/tertutup)
- Pencatatan otomatis data masuk ke database (waktu, foto, plat nomor)
  - Backup otomatis data secara berkala
  - Sinkronisasi dengan server pusat (opsional)

### 2. Pintu Keluar (Operator)
- Scan barcode tiket oleh operator
  - Mendukung berbagai jenis barcode scanner
  - Validasi tiket secara real-time
  - Deteksi tiket duplikat atau tidak valid
- Perhitungan biaya otomatis berdasarkan durasi
  - Tarif progresif yang dapat dikonfigurasi
  - Dukungan untuk berbagai jenis kendaraan
  - Diskon dan tarif khusus untuk member
  - Perhitungan overtime dan denda
- Verifikasi pembayaran oleh operator
  - Interface kasir yang user-friendly
  - Pencatatan metode pembayaran
  - Manajemen shift dan laporan per operator
- Cetak struk pembayaran
  - Format struk yang dapat dikustomisasi
  - Informasi lengkap transaksi
  - Logo dan header/footer yang dapat disesuaikan
- Gate terbuka setelah pembayaran selesai
  - Kontrol manual oleh operator
  - Timeout keamanan
  - Log aktivitas gate

### 3. Monitoring Real-time
- Status pintu masuk dan keluar
  - Dashboard real-time dengan SignalR
  - Notifikasi masalah perangkat
  - Statistik penggunaan
- Status perangkat (kamera, printer, push button)
  - Monitoring kesehatan perangkat
  - Auto-recovery untuk masalah umum
  - Log detail status perangkat
- Riwayat transaksi terkini
  - Update real-time
  - Filter dan pencarian
  - Export data
- Riwayat kendaraan dengan filter:
  - Status (Masuk/Keluar)
  - Rentang waktu
  - Jenis kendaraan
  - Plat nomor
  - Operator yang menangani
  - Metode pembayaran
- Notifikasi kejadian penting
  - Email alerts
  - Notifikasi di dashboard
  - Konfigurasi threshold dan kondisi alert
- Monitoring jumlah kendaraan
  - Kapasitas real-time
  - Prediksi kepenuhan
  - Statistik penggunaan

### 4. Manajemen
- Kelola slot parkir
  - Pemetaan area parkir
  - Monitoring okupansi
  - Reservasi slot (opsional)
- Kelola operator dan shift
  - Manajemen user dan role
  - Jadwal shift
  - Performance tracking
- Konfigurasi tarif parkir
  - Tarif per jenis kendaraan
  - Tarif progresif
  - Tarif khusus (event, member)
  - Diskon dan promo
- Pengaturan kamera dan printer
  - Konfigurasi perangkat
  - Kalibrasi dan testing
  - Backup konfigurasi
- Riwayat lengkap kendaraan:
  - Pencarian berdasarkan status masuk/keluar
  - Filter berdasarkan tanggal
  - Export data ke Excel/PDF
  - Detail transaksi per kendaraan
  - Foto kendaraan dan plat nomor
- Laporan dan statistik
  - Laporan harian/mingguan/bulanan
  - Analisis tren
  - Grafik dan visualisasi
  - Export dalam berbagai format

## Teknologi

### Backend
- **Framework**: ASP.NET Core 6.0
  - Clean Architecture
  - Dependency Injection
  - Async/await patterns
  - REST API
- **ORM**: Entity Framework Core 6.0
  - Code-first approach
  - Migration support
  - Query optimization
- **Database**: PostgreSQL 13+
  - Full-text search
  - JSON support
  - Backup/restore tools
- **Real-time**: SignalR
  - WebSocket transport
  - Fallback mechanisms
  - Scale-out support
- **Caching**: Redis (opsional)
  - Distributed caching
  - Session management
  - Real-time counters

### Frontend
- **Framework**: Bootstrap 5
  - Responsive design
  - Dark/light mode
  - Custom components
- **JavaScript**: jQuery
  - AJAX calls
  - DOM manipulation
  - Event handling
- **Real-time Updates**: SignalR client
  - Auto-reconnect
  - Connection management
  - Event handling
- **UI Components**:
  - DataTables
  - Chart.js
  - Select2
  - SweetAlert2
  - Moment.js

### Hardware Integration
- **Kamera**:
  - DirectShow untuk USB cameras
  - ONVIF untuk IP cameras
  - RTSP streaming support
  - Image processing
- **Printer**:
  - ESC/POS commands
  - Network printing
  - Multiple printer support
- **Input Devices**:
  - Serial port communication
  - GPIO integration
  - USB HID devices
- **Gate Control**:
  - RS232/RS485 protocols
  - Relay control
  - Safety features

## Persyaratan Sistem

### Software
- Windows 10/11 atau Linux (Ubuntu 20.04+)
- .NET 6.0 SDK
- PostgreSQL 13+
- Nginx/Apache (untuk production)
- Modern web browser (Chrome/Firefox/Edge)

### Hardware
- CPU: Intel Core i5 atau lebih tinggi
- RAM: Minimal 8GB
- Storage: 256GB SSD
- Network: Gigabit Ethernet
- USB ports untuk perangkat
- Serial ports atau USB-to-Serial adapter

### Perangkat Pendukung
- Kamera USB/IP dengan resolusi minimal 1080p
- Thermal printer 80mm
- Barcode scanner (1D/2D)
- Push button dan sensor
- Gate controller
- UPS untuk backup power

## Instalasi

### 1. Persiapan Sistem
```bash
# Update sistem
sudo apt update && sudo apt upgrade -y

# Install .NET 6.0
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y apt-transport-https
sudo apt install -y dotnet-sdk-6.0

# Install PostgreSQL
sudo apt install postgresql postgresql-contrib
```

### 2. Setup Database
```bash
# Buat user dan database
sudo -u postgres psql
CREATE USER parkirc WITH PASSWORD 'your_password';
CREATE DATABASE parkirc_db OWNER parkirc;
\q

# Backup database (jika perlu)
pg_dump -U parkirc parkirc_db > backup.sql
```

### 3. Clone dan Build Project
```bash
# Clone repository
git clone https://github.com/idiarsopgl/parkir28Mar.git
cd parkir28Mar

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Update database
dotnet ef database update
```

### 4. Konfigurasi Aplikasi
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=parkirc_db;Username=parkirc;Password=your_password"
  },
  "Hardware": {
    "Camera": {
      "Type": "DirectShow", // atau "IPCamera"
      "DeviceIndex": 0,
      "Resolution": "1920x1080",
      "FrameRate": 30
    },
    "Printer": {
      "Type": "Network",
      "Address": "192.168.1.100",
      "Port": 9100
    },
    "Gate": {
      "ComPort": "COM1",
      "BaudRate": 9600
    }
  },
  "Storage": {
    "ImagePath": "D:\\ParkingImages",
    "BackupPath": "D:\\Backups"
  }
}
```

### 5. Setup Service
Buat service file `/etc/systemd/system/parkirc.service`:
```ini
[Unit]
Description=ParkIRC Parking Management System
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/parkirc
ExecStart=/usr/bin/dotnet /opt/parkirc/ParkIRC.dll
Restart=always
RestartSec=10
SyslogIdentifier=parkirc
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

### 6. Jalankan Aplikasi
```bash
# Development
dotnet run

# Production
sudo systemctl enable parkirc
sudo systemctl start parkirc
```

## Konfigurasi Hardware

### 1. Kamera
#### USB Camera (DirectShow)
```csharp
// Contoh kode inisialisasi
var camera = new DirectShowCamera();
await camera.Initialize(0, 1920, 1080);
camera.StartCapture();
```

#### IP Camera
```csharp
// Contoh kode koneksi
var camera = new IPCamera("rtsp://admin:password@192.168.1.100:554/stream1");
await camera.Connect();
```

### 2. Printer
#### Network Printer
```csharp
// Contoh kode cetak
using var printer = new NetworkPrinter("192.168.1.100", 9100);
printer.PrintTicket(new TicketData { /* ... */ });
```

#### USB Printer
```csharp
// Contoh kode cetak
using var printer = new USBPrinter("COM3");
printer.Initialize();
printer.PrintReceipt(new ReceiptData { /* ... */ });
```

### 3. Gate Controller
```csharp
// Contoh kode kontrol gate
using var gate = new GateController("COM1", 9600);
await gate.Open();  // Buka gate
await Task.Delay(5000);  // Tunggu 5 detik
await gate.Close(); // Tutup gate
```

## Pemecahan Masalah

### Database
- **Koneksi Gagal**
  ```bash
  # Check service
  sudo systemctl status postgresql
  
  # Check logs
  sudo tail -f /var/log/postgresql/postgresql-13-main.log
  ```

### Kamera
- **Device Not Found**
  ```csharp
  // List available devices
  foreach (var device in DirectShowCamera.GetDevices())
  {
      Console.WriteLine($"Index: {device.Index}, Name: {device.Name}");
  }
  ```

### Printer
- **Print Error**
  ```bash
  # Check printer status
  lpstat -p
  
  # Clear print queue
  cancel -a
  ```

### Gate
- **Communication Error**
  ```csharp
  // Test serial port
  using var port = new SerialPort("COM1", 9600);
  port.Open();
  port.WriteLine("TEST");
  var response = port.ReadLine();
  ```

## Maintenance

### 1. Backup Database
```bash
# Automatic backup script
#!/bin/bash
BACKUP_DIR="/backup/database"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
pg_dump -U parkirc parkirc_db > $BACKUP_DIR/backup_$TIMESTAMP.sql
```

### 2. Log Rotation
```bash
# /etc/logrotate.d/parkirc
/var/log/parkirc/*.log {
    daily
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data www-data
}
```

### 3. Monitoring
```bash
# Check service status
sudo systemctl status parkirc

# View logs
sudo journalctl -u parkirc -f

# Monitor system resources
htop
```

## Keamanan

### 1. Database
- Gunakan strong passwords
- Batasi akses IP
- Regular security updates
- Encrypted connections

### 2. Application
- HTTPS enforcement
- JWT authentication
- Role-based access control
- Input validation
- XSS protection
- CSRF protection

### 3. Network
- Firewall configuration
- VPN for remote access
- Regular security audits
- Network segmentation

## Kontribusi

1. Fork repository
2. Buat branch fitur (`git checkout -b feature/NamaFitur`)
3. Commit perubahan (`git commit -m 'Menambahkan fitur baru'`)
4. Push ke branch (`git push origin feature/NamaFitur`)
5. Buat Pull Request

### Coding Standards
- Gunakan C# coding conventions
- Dokumentasi XML untuk public APIs
- Unit tests untuk logika bisnis
- Integration tests untuk APIs
- End-to-end tests untuk workflows

## Lisensi

Sistem ini didistribusikan di bawah Lisensi MIT. Lihat file `LICENSE` untuk informasi lebih lanjut.

## Kontak

ParkIRC Team - support@parkirc.com

Project Link: [https://github.com/idiarsopgl/parkir28Mar](https://github.com/idiarsopgl/parkir28Mar)

## Changelog

### v1.0.0 (2024-03-28)
- Initial release
- Basic parking management features
- Camera integration
- Printer support
- Gate control

### v1.1.0 (Coming Soon)
- Enhanced ANPR accuracy
- Mobile app integration
- Member management
- Advanced reporting
