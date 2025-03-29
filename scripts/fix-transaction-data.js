const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function fixTransactionsData() {
  try {
    console.log('Mencari kendaraan yang terparkir...');
    const parkedVehicles = await prisma.vehicles.findMany({
      where: {
        IsParked: true
      }
    });

    console.log(`Ditemukan ${parkedVehicles.length} kendaraan terparkir`);

    for (const vehicle of parkedVehicles) {
      console.log(`Memeriksa transaksi untuk kendaraan ${vehicle.VehicleNumber} (ID: ${vehicle.Id})`);
      
      // Cek apakah transaksi sudah ada
      const existingTransaction = await prisma.parkingTransactions.findFirst({
        where: {
          VehicleId: vehicle.Id,
          Status: 'Active'
        }
      });

      if (existingTransaction) {
        console.log(`Transaksi sudah ada untuk kendaraan ${vehicle.VehicleNumber}: ${existingTransaction.TransactionNumber}`);
        
        // Update fields yang mungkin kosong
        await prisma.parkingTransactions.update({
          where: {
            Id: existingTransaction.Id
          },
          data: {
            TicketNumber: existingTransaction.TicketNumber || vehicle.TicketNumber,
            VehicleNumber: vehicle.VehicleNumber,
            VehicleType: vehicle.VehicleType,
            ParkingSpaceId: vehicle.ParkingSpaceId || 1,
            PaymentStatus: 'Pending',
            PaymentMethod: 'Cash',
            EntryPoint: existingTransaction.EntryPoint || 'GATE',
            IsManualEntry: existingTransaction.IsManualEntry || false
          }
        });
        console.log(`Transaksi diperbarui: ${existingTransaction.TransactionNumber}`);
      } else {
        console.log(`Tidak ada transaksi aktif untuk kendaraan ${vehicle.VehicleNumber}, membuat transaksi baru`);
        
        // Dapatkan data parking space
        let parkingSpaceId = vehicle.ParkingSpaceId;
        if (!parkingSpaceId) {
          // Cari parking space kosong atau buat baru jika diperlukan
          const availableSpace = await prisma.parkingSpaces.findFirst({
            where: {
              IsOccupied: false,
              IsActive: true
            }
          });
          
          if (availableSpace) {
            parkingSpaceId = availableSpace.Id;
            console.log(`Menggunakan parking space yang tersedia: ${availableSpace.SpaceNumber} (ID: ${parkingSpaceId})`);
          } else {
            // Gunakan default ID 1 jika tidak ada
            parkingSpaceId = 1;
            console.log(`Tidak ada parking space tersedia, menggunakan default ID 1`);
          }
        }
        
        // Buat transaksi baru
        const ticketNumber = vehicle.TicketNumber || `TKT${Date.now()}${Math.floor(Math.random() * 1000)}`;
        const transactionNumber = `TRX-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-${Math.floor(Math.random() * 1000000).toString().padStart(6, '0')}`;
        
        const newTransaction = await prisma.parkingTransactions.create({
          data: {
            Id: transactionNumber,
            TransactionNumber: transactionNumber,
            TicketNumber: ticketNumber,
            VehicleId: vehicle.Id,
            ParkingSpaceId: parkingSpaceId,
            EntryTime: vehicle.EntryTime || new Date(),
            VehicleNumber: vehicle.VehicleNumber,
            VehicleType: vehicle.VehicleType || 'Unknown',
            Status: 'Active',
            PaymentStatus: 'Pending',
            PaymentMethod: 'Cash',
            HourlyRate: 5000,
            Amount: 0,
            TotalAmount: 0,
            PaymentAmount: 0,
            OperatorId: 'System',
            EntryPoint: 'FIXED',
            ImagePath: '',
            VehicleImagePath: '',
            IsOfflineEntry: false,
            IsManualEntry: true
          }
        });
        
        console.log(`Transaksi baru dibuat: ${newTransaction.TransactionNumber}`);
      }
    }

    console.log('Semua kendaraan terparkir sudah diperiksa dan diperbaiki');
  } catch (error) {
    console.error('Error:', error);
  } finally {
    await prisma.$disconnect();
  }
}

fixTransactionsData(); 