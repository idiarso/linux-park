const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function main() {
  try {
    // Contoh query untuk mendapatkan semua parking space yang tersedia
    const availableSpaces = await prisma.parking_spaces.findMany({
      where: {
        status: 'AVAILABLE'
      }
    });
    
    console.log('Available parking spaces:');
    console.log(availableSpaces);
    
    // Contoh query untuk mendapatkan semua kendaraan yang masih aktif
    const activeVehicles = await prisma.vehicle.findMany({
      where: {
        isActive: true
      },
      include: {
        ParkingEntry: {
          where: {
            ticketStatus: 'ACTIVE'
          },
          take: 1,
          orderBy: {
            entryTime: 'desc'
          }
        }
      }
    });
    
    console.log('Active vehicles currently parked:');
    console.log(activeVehicles.filter(v => v.ParkingEntry.length > 0));
    
  } catch (error) {
    console.error('Error querying database:', error);
  } finally {
    await prisma.$disconnect();
  }
}

// Uncomment baris di bawah ini untuk menjalankan fungsi main
// main();

module.exports = { prisma, main }; 