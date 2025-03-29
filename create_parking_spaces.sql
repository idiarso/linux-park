INSERT INTO "ParkingSpaces" ("SpaceNumber", "SpaceType", "IsOccupied", "IsReserved", "LastOccupiedTime", "HourlyRate", "Location", "ReservedFor")
VALUES 
  /* Indonesian terms */
  ('A1', 'Mobil', false, false, null, 10000, 'Lantai 1', null),
  ('A2', 'Mobil', false, false, null, 10000, 'Lantai 1', null),
  ('A3', 'Mobil', false, false, null, 10000, 'Lantai 1', null),
  ('B1', 'Motor', false, false, null, 5000, 'Lantai 1', null),
  ('B2', 'Motor', false, false, null, 5000, 'Lantai 1', null),
  ('B3', 'Motor', false, false, null, 5000, 'Lantai 1', null),
  /* English terms to match the form */
  ('C1', 'Car', false, false, null, 10000, 'Lantai 1', null),
  ('C2', 'Car', false, false, null, 10000, 'Lantai 1', null),
  ('C3', 'Car', false, false, null, 10000, 'Lantai 1', null),
  ('M1', 'Motorcycle', false, false, null, 5000, 'Lantai 1', null),
  ('M2', 'Motorcycle', false, false, null, 5000, 'Lantai 1', null),
  ('M3', 'Motorcycle', false, false, null, 5000, 'Lantai 1', null),
  ('T1', 'Truck', false, false, null, 15000, 'Lantai 1', null),
  ('T2', 'Truck', false, false, null, 15000, 'Lantai 1', null); 