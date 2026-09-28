import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\wwwroot\js\booking.js'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_alert = "alert('Önümüzdeki 30 gün içinde uygun boş saat bulunamadı.');"
new_alert = '''Swal.fire({
                icon: 'warning',
                title: 'Uyarı',
                text: 'Önümüzdeki 30 gün içinde uygun boş saat bulunamadı.',
                confirmButtonColor: '#d4af37',
                confirmButtonText: 'Tamam',
                background: '#1a1a24',
                color: '#fff'
            });'''

content = content.replace(old_alert, new_alert)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)
