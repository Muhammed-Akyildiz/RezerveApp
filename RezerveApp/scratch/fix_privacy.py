import io
import re

path_reg = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Account\Register.cshtml'
with io.open(path_reg, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('<a href="#" class="form-link">Kullanım koşullarını</a> ve <a href="#" class="form-link">gizlilik politikasını</a>', '<a href="/Home/Terms" target="_blank" class="form-link">Kullanım koşullarını</a> ve <a href="/Home/Privacy" target="_blank" class="form-link">gizlilik politikasını</a>')

with io.open(path_reg, 'w', encoding='utf-8') as f:
    f.write(content)

path_idx = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Home\Index.cshtml'
with io.open(path_idx, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('href="#">Gizlilik Politikası', 'href="/Home/Privacy">Gizlilik Politikası')
content = content.replace('href="#">Kullanım Şartları', 'href="/Home/Terms">Kullanım Şartları')

with io.open(path_idx, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated links in Register and Index')
