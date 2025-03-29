#!/bin/bash

# Run barcode test application

cd "$(dirname "$0")"
echo "Menjalankan aplikasi test barcode..."
dotnet run --project AllBarcodeTest.csproj

exit 0 