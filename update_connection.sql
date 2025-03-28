UPDATE settings SET "PostgresConnectionString" = 'Host=localhost;Port=5432;Database=parkir2;Username=postgres;Password=postgres';

CREATE TABLE IF NOT EXISTS parking_settings (
    id SERIAL PRIMARY KEY,
    site_name VARCHAR(255) NOT NULL,
    theme_color VARCHAR(50) DEFAULT '#007bff',
    postgres_connection_string TEXT
);
