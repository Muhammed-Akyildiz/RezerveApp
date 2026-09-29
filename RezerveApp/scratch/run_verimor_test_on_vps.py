import paramiko

hostname = "164.37.236.251"
username = "root"
password = "nEG6R5TzvX2Efjfh!"

ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect(hostname, username=username, password=password)

script = """
import requests
import json

url = "https://sms.verimor.com.tr/v2/send.json"
username = "908502411603"
password = "SHM?584jdd"

headers_to_test = [
    "Muhammed Aky\\u0131ld\\u0131z",
    "REZERVEAPP",
    "908502411603",
    "08502411603",
    "8502411603",
    "85024116030"
]

for sender in headers_to_test:
    payload = {
        "username": username,
        "password": password,
        "source_addr": sender,
        "messages": [
            {
                "msg": "Test message",
                "dest": "905369917452"
            }
        ]
    }
    
    response = requests.post(url, json=payload)
    print(f"Testing {sender} -> {response.status_code} : {response.text}")
"""

sftp = ssh.open_sftp()
with sftp.open('/root/test.py', 'w') as f:
    f.write(script)
sftp.close()

stdin, stdout, stderr = ssh.exec_command("python3 /root/test.py", get_pty=True)
print(stdout.read().decode('utf-8'))
ssh.close()
