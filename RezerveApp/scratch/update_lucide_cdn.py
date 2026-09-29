import os
import glob

old_str = "https://unpkg.com/lucide@latest/dist/umd/lucide.min.js"
new_str = "https://cdn.jsdelivr.net/npm/lucide@0.447.0/dist/umd/lucide.min.js"

old_str2 = "https://unpkg.com/lucide@latest"
new_str2 = "https://cdn.jsdelivr.net/npm/lucide@0.447.0/dist/umd/lucide.min.js"

base_dir = r"c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp"
for filepath in glob.glob(os.path.join(base_dir, "**", "*.cshtml"), recursive=True):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    if old_str in content or old_str2 in content:
        content = content.replace(old_str, new_str)
        content = content.replace(old_str2, new_str2)
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated", filepath)
