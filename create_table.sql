CREATE TABLE IF NOT EXISTS public."VehicleExit" (
    "Id" SERIAL PRIMARY KEY,
    "TicketNumber" VARCHAR(50) NOT NULL,
    "VehicleNumber" VARCHAR(20),
    "ExitTime" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PrintCount" INTEGER DEFAULT 0,
    "OperatorId" VARCHAR(50),
    "Notes" TEXT
);
