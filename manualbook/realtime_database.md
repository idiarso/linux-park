# Dokumentasi Fitur Database Realtime dan Notifikasi Koneksi

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [WebSocket dan Database Realtime](#websocket-dan-database-realtime)
3. [Indikator Status Koneksi](#indikator-status-koneksi)
4. [Mode Offline](#mode-offline)
5. [Sinkronisasi Data](#sinkronisasi-data)
6. [Pemecahan Masalah](#pemecahan-masalah)

## Pendahuluan

Sistem Parkir Modern dilengkapi dengan fitur database realtime dan sistem notifikasi koneksi yang memungkinkan aplikasi tetap berfungsi meskipun koneksi database terputus. Dokumen ini menjelaskan cara menggunakan fitur-fitur tersebut.

## WebSocket dan Database Realtime

Sistem Parkir Modern menggunakan teknologi WebSocket untuk memungkinkan komunikasi dua arah antara server dan aplikasi klien. Hal ini memungkinkan:

1. **Update Data Realtime**: Perubahan data pada satu aplikasi akan langsung terlihat di aplikasi lain yang terhubung
2. **Notifikasi Instan**: Pemberitahuan langsung saat kendaraan masuk atau keluar
3. **Status Sistem**: Pemantauan kondisi server dan database secara realtime

WebSocket server berjalan di port 8181 dan secara otomatis dimulai ketika Anda menjalankan komponen Server dari menu utama.

## Indikator Status Koneksi

Semua aplikasi klien (ParkingIN, ParkingOut, dan SimpleParkingAdmin) dilengkapi dengan indikator status koneksi database yang terletak di pojok kanan atas window aplikasi.

### Status Indikator:

1. **Memeriksa Koneksi** (Abu-abu): Aplikasi sedang memeriksa koneksi ke database
2. **Database Terhubung** (Hijau): Koneksi ke database aktif dan berfungsi normal
3. **Database Terputus** (Merah): Koneksi ke database terputus, aplikasi berjalan dalam mode offline

Indikator ini akan secara otomatis memeriksa status koneksi setiap 10 detik dan memperbarui tampilan sesuai kondisi terkini.

![Indikator Status Koneksi](../Images/connection_indicator.png)

## Mode Offline

Ketika koneksi database terputus (misalnya karena masalah jaringan atau server database mati), aplikasi akan secara otomatis beralih ke mode offline. Dalam mode ini:

### ParkingIN (Aplikasi Masuk Kendaraan):
- Tetap dapat memproses kendaraan yang masuk
- Data disimpan secara lokal di file JSON
- Data akan disinkronkan ke database saat koneksi tersedia kembali
- Foto kendaraan tetap disimpan di folder lokal

### ParkingOut (Aplikasi Keluar Kendaraan):
- Tidak dapat mencari data kendaraan yang masuk
- Tetap dapat memproses kendaraan keluar jika datanya sudah ditemukan sebelumnya
- Data keluar kendaraan disimpan secara lokal
- Data akan disinkronkan ke database saat koneksi tersedia kembali

### SimpleParkingAdmin (Panel Admin):
- Sebagian besar fitur tidak tersedia dalam mode offline
- Dapat melihat log dan statistik yang sudah dimuat sebelumnya
- Tidak dapat mengubah konfigurasi sistem

## Sinkronisasi Data

Ketika koneksi database tersedia kembali setelah periode offline:

1. Indikator status koneksi akan berubah menjadi hijau
2. Jika terdapat data offline, sistem akan menampilkan dialog konfirmasi untuk menyinkronkan data
3. Anda dapat memilih untuk sinkronisasi sekarang atau nanti

![Dialog Sinkronisasi](../Images/sync_dialog.png)

Proses sinkronisasi:
- Data offline akan dikirim ke database satu per satu
- Dialog progres akan ditampilkan selama proses berlangsung
- Setelah selesai, ringkasan sinkronisasi akan ditampilkan

**Catatan**: Jika proses sinkronisasi gagal, data offline tetap tersimpan dan Anda dapat mencoba sinkronisasi lagi nanti.

## Penyimpanan Data Offline

Data offline disimpan dalam:

1. **Folder offline_data**: Berisi file JSON untuk setiap transaksi
   - Format: `parking_entry_[tanggal]_[waktu]_[id].json` untuk data masuk
   - Format: `parking_exit_[tanggal]_[waktu]_[id].json` untuk data keluar

2. **Folder Images**: Menyimpan foto kendaraan
   - Subfolder `Masuk`: Foto kendaraan masuk
   - Subfolder `Keluar`: Foto kendaraan keluar

Data ini akan dipelihara otomatis oleh aplikasi dan dibersihkan setelah berhasil disinkronkan ke database.

## Pemecahan Masalah

### Koneksi Sering Terputus
1. Periksa koneksi jaringan antara aplikasi klien dan server
2. Pastikan layanan MySQL berjalan di server
3. Periksa konfigurasi kredensial database
4. Periksa log aplikasi di folder `logs`

### Data Tidak Tersinkronisasi
1. Buka aplikasi dan periksa status koneksi
2. Pastikan koneksi database aktif (indikator hijau)
3. Pilih opsi "Sinkronisasi Data" dari menu (jika tersedia)
4. Periksa folder `offline_data` untuk memastikan data tersimpan dengan benar

### Data Duplikat
Jika terjadi data duplikat setelah sinkronisasi:
1. Gunakan panel admin untuk mengidentifikasi dan menghapus data duplikat
2. Periksa log sinkronisasi untuk menemukan penyebab duplikasi

### Error Saat Sinkronisasi
Jika terjadi error saat sinkronisasi:
1. Catat pesan error yang muncul
2. Periksa log aplikasi untuk detail lebih lanjut
3. Selesaikan masalah yang teridentifikasi (misalnya isu koneksi)
4. Coba sinkronisasi ulang

Untuk bantuan lebih lanjut, hubungi administrator sistem atau lihat [Manual Administrator Server](administrator_server_guide.md).

# Panduan Database Realtime PostgreSQL

## Daftar Isi
1. [Pendahuluan](#pendahuluan)
2. [Konfigurasi PostgreSQL](#konfigurasi-postgresql)
3. [Monitoring Database](#monitoring-database)
4. [Backup dan Recovery](#backup-dan-recovery)
5. [Optimasi Performa](#optimasi-performa)
6. [Troubleshooting](#troubleshooting)

## Pendahuluan

Dokumen ini menjelaskan setup dan pengelolaan database PostgreSQL untuk sistem parkir. Database ini menangani transaksi realtime dan menyimpan data operasional sistem.

## Konfigurasi PostgreSQL

### Koneksi Database
```ini
Host: localhost
Port: 5432
Database: parkir2
Username: postgres
Password: postgres
```

### Connection String
```
"Host=localhost;Port=5432;Database=parkir2;Username=postgres;Password=postgres"
```

### Setup Awal Database

1. **Login ke PostgreSQL**:
   ```bash
   # Windows
   psql -U postgres
   
   # Linux
   sudo -u postgres psql
   ```

2. **Buat Database**:
   ```sql
   CREATE DATABASE parkir2;
   ```

3. **Set Password**:
   ```sql
   ALTER USER postgres WITH PASSWORD 'postgres';
   ```

4. **Berikan Hak Akses**:
   ```sql
   GRANT ALL PRIVILEGES ON DATABASE parkir2 TO postgres;
   ```

## Monitoring Database

### Query Monitoring

1. **Cek Koneksi Aktif**:
   ```sql
   SELECT * FROM pg_stat_activity;
   ```

2. **Cek Ukuran Database**:
   ```sql
   SELECT pg_size_pretty(pg_database_size('parkir2'));
   ```

3. **Monitoring Transaksi**:
   ```sql
   SELECT state, count(*) FROM pg_stat_activity GROUP BY state;
   ```

### Performance Monitoring

1. **Index Usage**:
   ```sql
   SELECT 
       schemaname, tablename, indexname, 
       idx_scan as number_of_scans,
       idx_tup_read as tuples_read,
       idx_tup_fetch as tuples_fetched
   FROM pg_stat_user_indexes;
   ```

2. **Table Statistics**:
   ```sql
   SELECT 
       relname as table_name,
       n_live_tup as live_rows,
       n_dead_tup as dead_rows
   FROM pg_stat_user_tables;
   ```

## Backup dan Recovery

### Backup Manual

1. **Full Database Backup**:
   ```bash
   # Windows
   pg_dump -U postgres -F c parkir2 > "%USERPROFILE%\Documents\Backup\parkir2_%date:~-4%%date:~3,2%%date:~0,2%.backup"

   # Linux
   pg_dump -U postgres -F c parkir2 > ~/backup/parkir2_$(date +%Y%m%d).backup
   ```

2. **Backup Specific Tables**:
   ```bash
   pg_dump -U postgres -t table_name parkir2 > table_backup.sql
   ```

### Automated Backup

1. **Windows Task Scheduler**:
   ```powershell
   # Create backup script
   @echo off
   set PGPASSWORD=postgres
   set BACKUP_PATH=%USERPROFILE%\Documents\Backup
   pg_dump -U postgres -F c parkir2 > "%BACKUP_PATH%\parkir2_%date:~-4%%date:~3,2%%date:~0,2%.backup"
   ```

2. **Linux Cron Job**:
   ```bash
   # Add to crontab
   0 3 * * * pg_dump -U postgres -F c parkir2 > /home/user/backup/parkir2_$(date +\%Y\%m\%d).backup
   ```

### Recovery

1. **Full Database Restore**:
   ```bash
   # Windows/Linux
   pg_restore -U postgres -d parkir2 backup_file.backup
   ```

2. **Selective Restore**:
   ```bash
   pg_restore -U postgres -t table_name -d parkir2 backup_file.backup
   ```

## Optimasi Performa

### Index Management

1. **Create Indexes**:
   ```sql
   -- Index untuk pencarian kendaraan
   CREATE INDEX idx_kendaraan_plat ON kendaraan(plat_nomor);
   
   -- Index untuk transaksi
   CREATE INDEX idx_transaksi_tanggal ON transaksi(tanggal);
   ```

2. **Analyze Tables**:
   ```sql
   ANALYZE kendaraan;
   ANALYZE transaksi;
   ```

### Maintenance

1. **Vacuum Database**:
   ```sql
   VACUUM ANALYZE;
   ```

2. **Reindex Tables**:
   ```sql
   REINDEX TABLE kendaraan;
   REINDEX TABLE transaksi;
   ```

## Troubleshooting

### Common Issues

1. **Connection Refused**:
   - Check PostgreSQL service status
   - Verify port 5432 is open
   - Check pg_hba.conf configuration

2. **Slow Queries**:
   - Use EXPLAIN ANALYZE
   - Check index usage
   - Verify table statistics

3. **High CPU Usage**:
   - Check for long-running queries
   - Analyze query plans
   - Review connection pool settings

### Query Optimization

1. **Identify Slow Queries**:
   ```sql
   SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
   FROM pg_stat_activity 
   WHERE pg_stat_activity.query != ''::text 
   ORDER BY duration DESC;
   ```

2. **Kill Long Running Queries**:
   ```sql
   SELECT pg_terminate_backend(pid) 
   FROM pg_stat_activity 
   WHERE now() - pg_stat_activity.query_start > interval '5 minutes';
   ```

### Maintenance Commands

1. **Reset Statistics**:
   ```sql
   SELECT pg_stat_reset();
   ```

2. **Clear Cache**:
   ```sql
   DISCARD ALL;
   ```

3. **Check Table Bloat**:
   ```sql
   SELECT 
       schemaname, tablename, 
       pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) as total_size
   FROM pg_tables
   WHERE schemaname = 'public'
   ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;
   ``` 