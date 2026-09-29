import paramiko
import sys

hostname = "164.37.236.251"
username = "root"
password = "nEG6R5TzvX2Efjfh!"

print("Connecting to VPS to update app...")
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
try:
    ssh.connect(hostname, username=username, password=password, timeout=10)
    
    commands = """
    cd /var/www/rezerveapp/RezerveApp
    git pull
    dotnet publish -c Release -o /var/www/rezerveapp/publish
    systemctl restart rezerveapp.service
    """
    stdin, stdout, stderr = ssh.exec_command(commands, get_pty=True)
    for line in iter(lambda: stdout.readline(), ""):
        sys.stdout.buffer.write(line.encode('utf-8', errors='replace'))
        sys.stdout.flush()
        
    print("DONE! App updated and restarted.")
except Exception as e:
    print(f"Error: {e}")
finally:
    ssh.close()
