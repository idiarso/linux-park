# Integrasi Prisma dengan ParkIRC

Dokumentasi ini menjelaskan cara menggunakan Prisma ORM yang diintegrasikan ke dalam aplikasi ParkIRC.

## Apa itu Prisma?

Prisma adalah ORM (Object-Relational Mapping) modern untuk Node.js dan TypeScript yang menyediakan akses database yang aman dan nyaman. Prisma memudahkan pengembang untuk melakukan operasi database dengan mencegah SQL Injection dan menyediakan auto-completion yang kuat.

## Struktur Integrasi

Integrasi Prisma dengan aplikasi ASP.NET Core ParkIRC terdiri dari beberapa komponen:

1. **Schema Prisma** - `prisma/schema.prisma`
   Mendefinisikan struktur database dan model-model yang digunakan.

2. **PrismaService** - `PrismaService.cs`
   Service yang memungkinkan aplikasi .NET untuk memanggil script Node.js yang menggunakan Prisma client.

3. **Script Node.js** - `scripts/`
   Berisi script JavaScript yang menggunakan Prisma client untuk mengakses database.

4. **Controller** - `Controllers/PrismaController.cs`
   Controller yang menggunakan PrismaService untuk mendapatkan data dari database.

## Catatan Penting tentang Penamaan

Perhatikan bahwa ada perbedaan penamaan tabel antara skema Prisma dan database PostgreSQL:

* **Database PostgreSQL** menggunakan **PascalCase** untuk nama tabel (contoh: `ParkingSpaces`, `Vehicles`, `AspNetUsers`)
* **Skema Prisma** umumnya menggunakan **snake_case** atau **camelCase** (contoh: `parking_spaces`, `vehicles`)

Saat menggunakan SQL mentah (`$queryRaw`) pastikan untuk menggunakan nama tabel dan kolom yang benar sesuai dengan database, termasuk tanda kutip ganda (`"`).

Contoh:
```javascript
const spaces = await prisma.$queryRaw`SELECT * FROM "ParkingSpaces" WHERE "IsOccupied" = false`;
```

## Cara Penggunaan

### 1. Mengakses Menu Prisma

Navigasikan ke `/Prisma/Index` untuk melihat halaman demo integrasi Prisma.

### 2. Membuat Script Node.js Baru

Untuk menambahkan operasi database baru, buat file JavaScript baru di folder `scripts/`:

```javascript
const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function myDatabaseOperation() {
  try {
    // Operasi database menggunakan SQL mentah (raw query)
    const result = await prisma.$queryRaw`
      SELECT * FROM "MyTable" 
      WHERE "MyColumn" = ${someValue}
    `;
    
    // Output harus dalam format JSON
    console.log(JSON.stringify(result));
    return result;
  } catch (error) {
    console.error(JSON.stringify({ error: error.message }));
    process.exit(1);
  } finally {
    await prisma.$disconnect();
  }
}

// Panggil fungsi utama
myDatabaseOperation();
```

### 3. Menggunakan dari Controller

```csharp
[HttpGet]
public async Task<IActionResult> MyData()
{
    try
    {
        string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "my-script.js");
        string result = await _prismaService.ExecutePrismaQuery(scriptPath);
        
        // Parse result as needed
        var data = System.Text.Json.JsonSerializer.Deserialize<object>(result);
        
        return View(data);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error retrieving data: {ex.Message}");
        return StatusCode(500, new { error = "Failed to retrieve data" });
    }
}
```

## Keuntungan Menggunakan Prisma

1. **Type Safety** - Prisma menghasilkan tipe yang sesuai dengan struktur database
2. **Query yang Aman** - Mencegah SQL Injection
3. **Auto-completion** - IntelliSense untuk struktur database
4. **Relasi yang Mudah** - Navigasi hubungan antar tabel dengan mudah
5. **Migrasi Database** - Mengelola perubahan skema dengan aman

## Pemecahan Masalah

Jika mengalami masalah dengan integrasi Prisma, periksa:

1. **Koneksi Database** - Pastikan database PostgreSQL berjalan dan dapat diakses
2. **Kredensial Database** - Pastikan user dan password di file .env benar
3. **Struktur Tabel** - Perhatikan perbedaan penamaan tabel antara skema Prisma dan database
4. **Node.js** - Pastikan Node.js versi 18 atau lebih tinggi terinstal
5. **Log** - Periksa log aplikasi untuk pesan error lebih detail

## Referensi

- [Dokumentasi Prisma](https://www.prisma.io/docs)
- [Prisma Client API](https://www.prisma.io/docs/reference/api-reference/prisma-client-reference) 