import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Booking\Index.cshtml'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace hero background
old_hero = '<section class="business-hero-banner" style="background-image: url(\'https://images.unsplash.com/photo-1503951914875-452162b0f3f1?auto=format&fit=crop&w=1920&q=80\');">'
new_hero = '<section class="business-hero-banner" style="background-image: url(\'@(string.IsNullOrEmpty(biz.GalleryImage1) ? "https://images.unsplash.com/photo-1503951914875-452162b0f3f1?auto=format&fit=crop&w=1920&q=80" : biz.GalleryImage1)\');">'
content = content.replace(old_hero, new_hero)

# Replace gallery 1
old_g1 = '<img src="https://images.unsplash.com/photo-1621605815971-fbc98d665033?auto=format&fit=crop&w=1200&q=85"'
new_g1 = '<img src="@(string.IsNullOrEmpty(biz.GalleryImage1) ? "https://images.unsplash.com/photo-1621605815971-fbc98d665033?auto=format&fit=crop&w=1200&q=85" : biz.GalleryImage1)"'
content = content.replace(old_g1, new_g1)

# Replace gallery 2
old_g2 = '<img src="https://images.unsplash.com/photo-1503951914875-452162b0f3f1?auto=format&fit=crop&w=700&q=85"'
new_g2 = '<img src="@(string.IsNullOrEmpty(biz.GalleryImage2) ? "https://images.unsplash.com/photo-1503951914875-452162b0f3f1?auto=format&fit=crop&w=700&q=85" : biz.GalleryImage2)"'
content = content.replace(old_g2, new_g2)

# Replace gallery 3
old_g3 = '<img src="https://images.unsplash.com/photo-1512690459411-b9245aed614b?auto=format&fit=crop&w=700&q=85"'
new_g3 = '<img src="@(string.IsNullOrEmpty(biz.GalleryImage3) ? "https://images.unsplash.com/photo-1512690459411-b9245aed614b?auto=format&fit=crop&w=700&q=85" : biz.GalleryImage3)"'
content = content.replace(old_g3, new_g3)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated Booking Index images.')
