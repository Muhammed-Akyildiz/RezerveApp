import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Views\Booking\Index.cshtml'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_rating = '''                <div class="hero-meta-row">
                    <span class="hero-rating">
                        <span class="stars">⭐ ⭐ ⭐ ⭐ ⭐ </span>
                        <strong>4.9</strong>
                        <span class="muted-text">| Müşteri memnuniyeti</span>
                    </span>
                </div>'''

new_rating = '''                <div class="hero-meta-row">
                    <span class="hero-rating">
                        @if (ViewBag.ReviewCount > 0)
                        {
                            <span class="stars">⭐</span>
                            <strong>@(((double)ViewBag.AvgRating).ToString("0.0"))</strong>
                            <span class="muted-text">| @ViewBag.ReviewCount Değerlendirme</span>
                        }
                        else
                        {
                            <span class="muted-text">Henüz değerlendirme yok</span>
                        }
                    </span>
                </div>'''

content = content.replace(old_rating, new_rating)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)
