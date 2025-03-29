-- Buat fungsi generate ID transaksi
CREATE OR REPLACE FUNCTION generate_transaction_id()
RETURNS TRIGGER AS 
$$
BEGIN
    NEW."Id" = 'TRX-' || to_char(NOW(), 'YYYYMMDD') || '-' || substr(md5(random()::text), 1, 6);
    RETURN NEW;
END;
$$ 
LANGUAGE plpgsql;

-- Buat trigger untuk otomatis mengisi ID saat NULL
DROP TRIGGER IF EXISTS set_transaction_id ON "ParkingTransactions";
CREATE TRIGGER set_transaction_id
BEFORE INSERT ON "ParkingTransactions"
FOR EACH ROW
WHEN (NEW."Id" IS NULL)
EXECUTE FUNCTION generate_transaction_id(); 