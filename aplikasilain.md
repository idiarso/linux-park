# Catatan Port yang Digunakan

## Port yang Sudah Digunakan

| Port | Aplikasi/Service | Deskripsi |
|------|-----------------|------------|
| 5050 | ParkIRC API Server | Server utama untuk aplikasi parkir (.NET) |
| 5051 | ParkIRC Python API | Server Python untuk integrasi tambahan |
| 5555 | Prisma Studio | Database management interface |
| 5432 | PostgreSQL | Database server |
| 8181 | WebSocket Server | Komunikasi realtime untuk status parkir |

## Panduan untuk Developer

1. **JANGAN menggunakan port-port di atas** untuk aplikasi baru Anda untuk menghindari konflik.

2. **Rekomendasi range port untuk aplikasi baru**:
   - Development: 3000-3999
   - API Services: 4000-4999
   - Admin Tools: 6000-6999
   - WebSocket/Realtime: 8000-8099 (kecuali 8181)

3. **Jika ingin integrasi dengan ParkIRC**:
   - Gunakan API endpoint: `http://192.168.2.6:5050/api`
   - Dokumentasi API tersedia di `API-DOCUMENTATION.md`
   - Hubungi admin sistem untuk mendapatkan credentials

4. **Konvensi Penamaan Port**:
   - Development: `3xxx`
   - Production: `4xxx`
   - Admin/Tools: `6xxx`
   - WebSocket: `8xxx`

5. **Sebelum Deploy**:
   - Pastikan port yang akan digunakan belum terpakai
   - Update dokumen ini dengan menambahkan port baru yang akan digunakan
   - Test koneksi untuk memastikan tidak ada konflik port

## Cara Update Dokumen Ini

Jika Anda akan menggunakan port baru untuk aplikasi Anda:

1. Fork repository ini
2. Update tabel port di atas dengan menambahkan port yang akan Anda gunakan
3. Tambahkan deskripsi yang jelas tentang aplikasi Anda
4. Submit pull request

## Kontak

Jika ada pertanyaan atau konflik port, hubungi:
- Admin Sistem: [email/kontak]
- Network Team: [email/kontak]

## Monitoring Port

Untuk melihat port yang sedang aktif di server:
```bash
# List semua port yang aktif
sudo netstat -tulpn | grep LISTEN

# Cek port spesifik (ganti PORT dengan nomor port)
sudo lsof -i :PORT
``` 