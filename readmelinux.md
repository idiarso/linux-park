# Linux Parking Management System Deployment Guide

## System Requirements

### Minimum Hardware Requirements
- CPU: Intel Core i5 or equivalent (2.0 GHz or higher)
- RAM: 8GB minimum (16GB recommended)
- Storage: 256GB SSD
- Network: Gigabit Ethernet
- USB Ports: At least 2 available ports for peripherals
- Serial Ports or USB-to-Serial adapter (for gate control)

### Software Requirements
- Ubuntu 20.04 LTS or higher / Debian 11 or higher
- .NET 6.0 SDK and Runtime
- PostgreSQL 13+
- Nginx (for production deployment)
- Git

## Installation Steps

### 1. System Preparation

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install required dependencies
sudo apt install -y curl libpng-dev libjpeg-dev libgdiplus nginx
```

### 2. Install .NET 6.0 SDK

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt update
sudo apt install -y apt-transport-https
sudo apt install -y dotnet-sdk-6.0
```

### 3. Install PostgreSQL

```bash
# Install PostgreSQL
sudo apt install -y postgresql postgresql-contrib

# Start PostgreSQL service
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Create database and user
sudo -u postgres psql -c "CREATE USER parkir WITH PASSWORD 'your_secure_password';"
sudo -u postgres psql -c "CREATE DATABASE parkir2 OWNER parkir;"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE parkir2 TO parkir;"
```

### 4. Application Deployment

```bash
# Create application directory
sudo mkdir -p /opt/parking
sudo chown -R $USER:$USER /opt/parking

# Clone repository
git clone https://github.com/idiarso/linux-park.git /opt/parking
cd /opt/parking

# Install dependencies and build
dotnet restore
dotnet build
dotnet publish -c Release -o /opt/parking/publish

# Set permissions
sudo chown -R www-data:www-data /opt/parking/publish
```

### 5. Configure Application Settings

Edit `appsettings.json` in the publish directory:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=parkir2;Username=parkir;Password=your_secure_password;",
  },
  "Hardware": {
    "PrinterName": "your_printer_name",
    "CameraIndex": 0,
    "SerialPort": "/dev/ttyUSB0"
  }
}
```

### 6. Setup Systemd Service

Create service file:

```bash
sudo nano /etc/systemd/system/parking.service
```

Add the following content:

```ini
[Unit]
Description=Parking Management System
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/parking/publish
ExecStart=/usr/bin/dotnet /opt/parking/publish/ParkIRC.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

### 7. Configure Nginx as Reverse Proxy

Create Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/parking
```

Add the following content:

```nginx
server {
    listen 80;
    server_name your_domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/parking /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 8. Start the Application

```bash
# Enable and start the service
sudo systemctl enable parking
sudo systemctl start parking

# Check status
sudo systemctl status parking
```

### 9. Hardware Setup

#### USB Camera
```bash
# List available video devices
ls -l /dev/video*

# Give permissions
sudo usermod -a -G video www-data
```

#### Serial Port (for gate control)
```bash
# Give permissions to serial port
sudo usermod -a -G dialout www-data

# Test serial port
sudo chmod 666 /dev/ttyUSB0
```

#### Printer
```bash
# Install CUPS for printer management
sudo apt install -y cups

# Add user to printer group
sudo usermod -a -G lpadmin www-data
```

## Maintenance

### Backup Database
```bash
# Create backup script
sudo nano /opt/parking/backup.sh
```

Add the following content:
```bash
#!/bin/bash
BACKUP_DIR="/opt/parking/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR
pg_dump -U parkir parkir2 > $BACKUP_DIR/backup_$TIMESTAMP.sql
find $BACKUP_DIR -type f -mtime +30 -delete
```

Make it executable and set up cron job:
```bash
sudo chmod +x /opt/parking/backup.sh
sudo crontab -e

# Add this line for daily backup at 2 AM
0 2 * * * /opt/parking/backup.sh
```

### Log Rotation
```bash
sudo nano /etc/logrotate.d/parking
```

Add the following content:
```
/opt/parking/logs/*.log {
    daily
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data www-data
    sharedscripts
    postrotate
        systemctl reload parking
    endscript
}
```

## Troubleshooting

### Service Issues
```bash
# Check service status
sudo systemctl status parking

# View logs
sudo journalctl -u parking -f

# Restart service
sudo systemctl restart parking
```

### Database Issues
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Check database logs
sudo tail -f /var/log/postgresql/postgresql-13-main.log
```

### Hardware Issues
```bash
# Check USB devices
lsusb

# Check serial ports
dmesg | grep tty

# Check printer status
lpstat -p
```

## Security Recommendations

1. **Firewall Configuration**
```bash
sudo ufw enable
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp
```

2. **SSL/TLS Setup**
```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Get SSL certificate
sudo certbot --nginx -d your_domain.com
```

3. **Regular Updates**
```bash
# Create update script
sudo nano /opt/parking/update.sh
```

Add:
```bash
#!/bin/bash
sudo apt update
sudo apt upgrade -y
sudo systemctl restart parking
```

## Support

For technical support, please contact:
- Email: support@your_domain.com
- Phone: Your support phone number

## License

This project is licensed under the MIT License - see the LICENSE file for details. 