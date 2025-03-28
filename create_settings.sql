DROP TABLE IF EXISTS parking_settings;

CREATE TABLE parking_settings (
    id SERIAL PRIMARY KEY,
    site_name VARCHAR(255) NOT NULL,
    logo_path TEXT,
    favicon_path TEXT,
    footer_text TEXT,
    theme_color VARCHAR(50) DEFAULT '#007bff',
    show_logo BOOLEAN DEFAULT true,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by TEXT,
    postgres_connection_string TEXT
);
