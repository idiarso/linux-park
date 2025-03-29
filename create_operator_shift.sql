-- Mendapatkan ID shift yang baru saja dibuat
DO $$
DECLARE
    shift_id INT;
    admin_id TEXT;
BEGIN
    -- Dapatkan ID shift aktif
    SELECT "Id" INTO shift_id FROM "Shifts" WHERE "IsActive" = true ORDER BY "CreatedAt" DESC LIMIT 1;
    
    -- Dapatkan ID user admin
    SELECT "Id" INTO admin_id FROM "AspNetUsers" WHERE "Email" = 'admin@parkingsystem.com' LIMIT 1;
    
    IF shift_id IS NOT NULL AND admin_id IS NOT NULL THEN
        -- Tambahkan operator shift
        INSERT INTO "OperatorShifts" ("OperatorsId", "ShiftsId")
        VALUES (admin_id, shift_id);
        
        RAISE NOTICE 'Operator shift created successfully';
    ELSE
        RAISE EXCEPTION 'Could not find shift or admin user';
    END IF;
END $$; 