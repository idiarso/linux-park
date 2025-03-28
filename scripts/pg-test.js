const { Client } = require('pg');

async function testPgConnection() {
  // Uji dengan password 1q2w3e4r5t
  const client1 = new Client({
    user: 'postgres',
    host: 'localhost',
    database: 'parkir2',
    password: '1q2w3e4r5t',
    port: 5432,
  });

  try {
    console.log('Trying to connect with password: 1q2w3e4r5t');
    await client1.connect();
    const res = await client1.query('SELECT current_database()');
    console.log('Connected successfully!');
    console.log('Current database:', res.rows[0].current_database);
    await client1.end();
    return true;
  } catch (err1) {
    console.error('Failed with password 1q2w3e4r5t:', err1.message);
    await client1.end().catch(() => {});
    
    // Uji dengan password postgres (default)
    const client2 = new Client({
      user: 'postgres',
      host: 'localhost',
      database: 'parkir2',
      password: 'postgres',
      port: 5432,
    });
    
    try {
      console.log('\nTrying to connect with password: postgres');
      await client2.connect();
      const res = await client2.query('SELECT current_database()');
      console.log('Connected successfully!');
      console.log('Current database:', res.rows[0].current_database);
      await client2.end();
      return true;
    } catch (err2) {
      console.error('Failed with password postgres:', err2.message);
      await client2.end().catch(() => {});
      
      // Uji tanpa password
      const client3 = new Client({
        user: 'postgres',
        host: 'localhost',
        database: 'parkir2',
        password: '',
        port: 5432,
      });
      
      try {
        console.log('\nTrying to connect without password');
        await client3.connect();
        const res = await client3.query('SELECT current_database()');
        console.log('Connected successfully!');
        console.log('Current database:', res.rows[0].current_database);
        await client3.end();
        return true;
      } catch (err3) {
        console.error('Failed without password:', err3.message);
        await client3.end().catch(() => {});
        return false;
      }
    }
  }
}

testPgConnection(); 