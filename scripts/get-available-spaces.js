const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient();

async function getAvailableSpaces() {
  try {
    const spaces = await prisma.parking_spaces.findMany({
      where: {
        status: 'AVAILABLE'
      },
      orderBy: {
        number: 'asc'
      }
    });
    
    console.log(JSON.stringify(spaces));
    return spaces;
  } catch (error) {
    console.error(JSON.stringify({ error: error.message }));
    process.exit(1);
  } finally {
    await prisma.$disconnect();
  }
}

getAvailableSpaces(); 