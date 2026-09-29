import paramiko
import json
import sys

hostname = "164.37.236.251"
username = "root"
password = "nEG6R5TzvX2Efjfh!"

print("Connecting to VPS...")
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
try:
    ssh.connect(hostname, username=username, password=password, timeout=10)
    
    print("Installing PostgreSQL...")
    commands = """
    export DEBIAN_FRONTEND=noninteractive
    apt-get update -y
    apt-get install -y postgresql postgresql-contrib
    systemctl enable postgresql
    systemctl start postgresql
    sudo -u postgres psql -tc "SELECT 1 FROM pg_roles WHERE rolname='rezerveuser'" | grep -q 1 || sudo -u postgres psql -c "CREATE USER rezerveuser WITH PASSWORD 'Rezerve2026DB!';"
    sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname='rezerveappdb'" | grep -q 1 || sudo -u postgres psql -c "CREATE DATABASE rezerveappdb OWNER rezerveuser;"
    sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE rezerveappdb TO rezerveuser;"
    sudo -u postgres psql -d rezerveappdb -c "GRANT ALL ON SCHEMA public TO rezerveuser;"
    """
    stdin, stdout, stderr = ssh.exec_command(commands, get_pty=True)
    for line in iter(lambda: stdout.readline(), ""):
        sys.stdout.buffer.write(line.encode('utf-8', errors='replace'))
        sys.stdout.flush()
        
    print("Modifying appsettings.json via SFTP...")
    sftp = ssh.open_sftp()
    appsettings_path = '/var/www/rezerveapp/publish/appsettings.json'
    
    with sftp.open(appsettings_path, 'r') as f:
        content = f.read().decode('utf-8')
        appsettings = json.loads(content)
        
    appsettings['ConnectionStrings']['DefaultConnection'] = "Host=localhost;Database=rezerveappdb;Username=rezerveuser;Password=Rezerve2026DB!;SSL Mode=Disable;Trust Server Certificate=true;"
    appsettings['Cloudinary']['Url'] = "cloudinary://813485812936144:9JpAwARrAFjZOkv3fZsJIiQOyiQ@zgxxse4i"
    
    with sftp.open(appsettings_path, 'w') as f:
        f.write(json.dumps(appsettings, indent=2).encode('utf-8'))
        
    sftp.close()
    
    print("Restarting app to apply database migrations...")
    stdin, stdout, stderr = ssh.exec_command("systemctl restart rezerveapp.service && sleep 5 && systemctl status rezerveapp.service --no-pager", get_pty=True)
    for line in iter(lambda: stdout.readline(), ""):
        sys.stdout.buffer.write(line.encode('utf-8', errors='replace'))
        sys.stdout.flush()
        
    print("DONE! Local PostgreSQL database is set up and app is running.")
    
except Exception as e:
    print(f"Error: {e}")
finally:
    ssh.close()
