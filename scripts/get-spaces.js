const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function getParkingSpaces() {
  try {
    // Menggunakan eksekusi SQL mentah untuk mengambil data dari tabel ParkingSpaces
    const spaces = await prisma.$queryRaw`
      SELECT * FROM "ParkingSpaces" 
      WHERE "IsOccupied" = false 
      ORDER BY "SpaceNumber" ASC
    `;
    
    console.log(JSON.stringify(spaces, null, 2));
    return spaces;
  } catch (error) {
    console.error(JSON.stringify({ error: error.message }));
    process.exit(1);
  } finally {
    await prisma.$disconnect();
  }
}

getParkingSpaces(); 