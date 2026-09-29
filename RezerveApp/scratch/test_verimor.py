import requests
import json

url = "https://sms.verimor.com.tr/v2/send.json"
username = "908502411603"
password = "SHM?584jdd"

headers_to_test = [
    "Muhammed Akyıldız",
    "REZERVEAPP",
    "908502411603",
    "08502411603",
    "8502411603"
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
    print(f"Testing SenderId: {sender}")
    print(f"Status: {response.status_code}")
    print(f"Response: {response.text}")
    print("-" * 30)
