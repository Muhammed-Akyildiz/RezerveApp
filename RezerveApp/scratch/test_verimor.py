import urllib.request
import json
import urllib.error

data = json.dumps({
    'username': '908502411603',
    'password': 'APJ!463fny',
    'source_addr': 'Muhammed Akyıldız',
    'messages': [{'msg': 'test', 'dest': '905320000000'}]
}).encode('utf-8')

req = urllib.request.Request('https://sms.verimor.com.tr/v2/send.json', data=data, headers={'Content-Type': 'application/json'})

try:
    response = urllib.request.urlopen(req)
    print("Success:", response.read().decode('utf-8'))
except urllib.error.HTTPError as e:
    print("Error Code:", e.code)
    print("Error Body:", e.read().decode('utf-8'))
except Exception as e:
    print("Exception:", str(e))
