INSERT INTO "Shifts" ("Name", "ShiftName", "Date", "StartTime", "EndTime", "Description", "MaxOperators", "IsActive", "WorkDays", "CreatedAt")
VALUES 
  ('Shift Pagi', 'Pagi', NOW(), NOW(), NOW() + INTERVAL '8 hours', 'Shift pagi aktif', 5, true, 'Mon,Tue,Wed,Thu,Fri,Sat,Sun', NOW()); 