const { PrismaClient } = require('@prisma/client');
const prisma = new PrismaClient({
  log: ['query', 'info', 'warn', 'error'],
});

async function testConnection() {
  try {
    // Mencoba melakukan query sederhana
    const result = await prisma.$queryRaw`SELECT 1 as test`;
    console.log('Connection successful!');
    console.log('Query result:', result);
    
    return true;
  } catch (error) {
    console.error('Connection error:', error.message);
    return false;
  } finally {
    await prisma.$disconnect();
  }
}

testConnection(); 