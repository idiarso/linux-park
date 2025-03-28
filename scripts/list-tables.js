const { Client } = require('pg');

async function listTables() {
  const client = new Client({
    user: 'postgres',
    host: 'localhost',
    database: 'parkir2',
    password: 'postgres',
    port: 5432,
  });

  try {
    await client.connect();
    console.log('Connected to database successfully!');
    
    // Query untuk mendapatkan daftar tabel
    const res = await client.query(`
      SELECT table_name 
      FROM information_schema.tables 
      WHERE table_schema = 'public'
      ORDER BY table_name;
    `);
    
    if (res.rows.length === 0) {
      console.log('No tables found in the database.');
    } else {
      console.log('Tables in the database:');
      res.rows.forEach((row, index) => {
        console.log(`${index + 1}. ${row.table_name}`);
      });
    }
    
    // Mencoba mengambil informasi database
    const dbInfo = await client.query(`
      SELECT current_database(), current_user, version();
    `);
    console.log('\nDatabase Information:');
    console.log(`Database: ${dbInfo.rows[0].current_database}`);
    console.log(`User: ${dbInfo.rows[0].current_user}`);
    console.log(`Version: ${dbInfo.rows[0].version}`);
    
  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await client.end();
  }
}

listTables(); 