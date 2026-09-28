import io

# 1. Update ForgotPassword.cshtml
path_fp = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Account\ForgotPassword.cshtml'
with io.open(path_fp, 'r', encoding='utf-8') as f:
    content_fp = f.read()

old_svg = '''<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="var(--color-primary)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                        <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
                    </svg>'''

new_svg = '''<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80" viewBox="0 0 24 24" fill="none" stroke="#d4af37" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                    </svg>'''

content_fp = content_fp.replace(old_svg, new_svg)

with io.open(path_fp, 'w', encoding='utf-8') as f:
    f.write(content_fp)


# 2. Update Maintenance.cshtml
path_m = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Home\Maintenance.cshtml'
with io.open(path_m, 'r', encoding='utf-8') as f:
    content_m = f.read()

m_svg = '''<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80" viewBox="0 0 24 24" fill="none" stroke="#d4af37" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"></path>
            </svg>'''

content_m = content_m.replace('🛠️', m_svg)

with io.open(path_m, 'w', encoding='utf-8') as f:
    f.write(content_m)

print('Icons fixed.')
