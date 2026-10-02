import paramiko
import sys

def run_ssh_command(host, user, password, command):
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    try:
        client.connect(hostname=host, username=user, password=password)
        stdin, stdout, stderr = client.exec_command(command)
        out = stdout.read()
        err = stderr.read()
        print(out.decode('utf-8', errors='replace').encode('ascii', errors='replace').decode('ascii'))
        if err:
            print("ERROR:", err.decode('utf-8', errors='replace').encode('ascii', errors='replace').decode('ascii'))
    except Exception as e:
        print("Exception:", str(e))
    finally:
        client.close()

if __name__ == "__main__":
    run_ssh_command("164.37.236.251", "root", "nEG6R5TzvX2Efjfh!", sys.argv[1])
