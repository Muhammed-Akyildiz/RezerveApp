import paramiko
import time
import sys

hostname = "164.37.236.251"
username = "root"
password = "nEG6R5TzvX2Efjfh!"

commands = """
set -e
export DEBIAN_FRONTEND=noninteractive

echo "==> Updating system..."
apt-get update -y
apt-get install -y nginx git curl apt-transport-https

echo "==> Installing .NET 8..."
wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update -y
apt-get install -y dotnet-sdk-8.0

echo "==> Setting up application directory..."
rm -rf /var/www/rezerveapp
mkdir -p /var/www/rezerveapp
cd /var/www/rezerveapp

echo "==> Cloning repository..."
git clone https://github.com/Muhammed-Akyildiz/RezerveApp.git .

echo "==> Publishing .NET application..."
cd RezerveApp
dotnet publish -c Release -o /var/www/rezerveapp/publish
chown -R www-data:www-data /var/www/rezerveapp/publish

echo "==> Creating Systemd service..."
cat > /etc/systemd/system/rezerveapp.service << 'EOF'
[Unit]
Description=RezerveApp .NET 8 Web App

[Service]
WorkingDirectory=/var/www/rezerveapp/publish
ExecStart=/usr/bin/dotnet /var/www/rezerveapp/publish/RezerveApp.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=rezerveapp
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
EOF

echo "==> Creating Nginx configuration..."
cat > /etc/nginx/sites-available/rezerveapp << 'EOF'
server {
    listen 80;
    server_name rezerveapp.com.tr www.rezerveapp.com.tr;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
EOF

ln -sf /etc/nginx/sites-available/rezerveapp /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

echo "==> Reloading services..."
systemctl daemon-reload
systemctl enable rezerveapp.service
systemctl restart rezerveapp.service
systemctl restart nginx

echo "==> Installing Certbot for free SSL..."
apt-get install -y python3-certbot-nginx
certbot --nginx -d rezerveapp.com.tr -d www.rezerveapp.com.tr --non-interactive --agree-tos -m muhammedakyildiz44@gmail.com || echo "Certbot failed, maybe DNS hasn't propagated yet."

echo "==> DEPLOYMENT COMPLETE! <=="
"""

print(f"Connecting to {hostname}...")
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())

try:
    ssh.connect(hostname, username=username, password=password, timeout=10)
    print("Connected successfully! Starting deployment script (this may take a few minutes)...")
    
    stdin, stdout, stderr = ssh.exec_command(commands, get_pty=True)
    
    for line in iter(lambda: stdout.readline(), ""):
        sys.stdout.buffer.write(line.encode('utf-8', errors='replace'))
        sys.stdout.flush()
        
    exit_status = stdout.channel.recv_exit_status()
    print(f"Deployment finished with exit status: {exit_status}")
    
except Exception as e:
    print(f"Error: {e}")
finally:
    ssh.close()
