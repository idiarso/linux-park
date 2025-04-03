#!/bin/bash

# Server monitoring script for Parking API
LOG_FILE="/home/tekno/Desktop/AAA/parkir28Mar/server-monitor.log"
API_URL="http://192.168.2.6:5050/api/test-connection"
DB_NAME="parkir2"
DB_USER="postgres"

echo "====== SERVER MONITORING ======" | tee -a $LOG_FILE
echo "Date: $(date)" | tee -a $LOG_FILE
echo "" | tee -a $LOG_FILE

# Check API server
echo "--- API Server Status ---" | tee -a $LOG_FILE
if curl -s -o /dev/null -w "%{http_code}" $API_URL | grep -q "200"; then
    echo "API server is UP and running" | tee -a $LOG_FILE
else
    echo "WARNING: API server is DOWN or not responding!" | tee -a $LOG_FILE
fi

# Check API server processes
echo "" | tee -a $LOG_FILE
echo "--- API Server Processes ---" | tee -a $LOG_FILE
ps aux | grep -i "dotnet" | grep -v grep | tee -a $LOG_FILE

# Check database connection
echo "" | tee -a $LOG_FILE
echo "--- Database Status ---" | tee -a $LOG_FILE
if sudo -u postgres psql -d $DB_NAME -c "SELECT 1;" >/dev/null 2>&1; then
    echo "Database is UP and accessible" | tee -a $LOG_FILE
    
    # Check vehicle count
    VEHICLE_COUNT=$(sudo -u postgres psql -d $DB_NAME -t -c "SELECT COUNT(*) FROM public.\"Vehicles\";")
    echo "Total vehicles in database: $VEHICLE_COUNT" | tee -a $LOG_FILE
    
    # Get recent entries
    echo "" | tee -a $LOG_FILE
    echo "Recent entries:" | tee -a $LOG_FILE
    sudo -u postgres psql -d $DB_NAME -c "SELECT \"Id\", \"VehicleNumber\", \"TicketNumber\" FROM public.\"Vehicles\" ORDER BY \"Id\" DESC LIMIT 5;" | tee -a $LOG_FILE
else
    echo "WARNING: Database is DOWN or not accessible!" | tee -a $LOG_FILE
fi

# Check disk space
echo "" | tee -a $LOG_FILE
echo "--- Disk Space ---" | tee -a $LOG_FILE
df -h | grep "/dev/sd" | tee -a $LOG_FILE

# Check memory usage
echo "" | tee -a $LOG_FILE
echo "--- Memory Usage ---" | tee -a $LOG_FILE
free -h | tee -a $LOG_FILE

# Finish
echo "" | tee -a $LOG_FILE
echo "====== MONITORING COMPLETE ======" | tee -a $LOG_FILE
echo "Log saved to $LOG_FILE"

# Fungsi untuk mengirim notifikasi (ganti dengan implementasi sesuai kebutuhan)
send_notification() {
    echo "Server down: $1"
    # Implementasi notifikasi (email, SMS, dll)
}

# Loop monitoring
while true; do
    # Test koneksi ke API
    if curl -s -o /dev/null -w "%{http_code}" $API_URL | grep -q "200"; then
        echo "[$(date)] Server OK"
    else
        echo "[$(date)] Server DOWN"
        send_notification "API tidak merespons"
    fi
    
    # Tunggu 1 menit sebelum check berikutnya
    sleep 60
done 