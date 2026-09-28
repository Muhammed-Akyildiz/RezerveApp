document.addEventListener('input', function(e) {
    if (!e.target || !e.target.tagName || e.target.tagName.toLowerCase() !== 'input') return;

    const nameAttr = e.target.name ? e.target.name.toLowerCase() : '';
    const typeAttr = e.target.type ? e.target.type.toLowerCase() : '';

    // Telefon no girilen yerlerde sadece rakam kullandırtma
    if (typeAttr === 'tel' || nameAttr.includes('phone') || nameAttr.includes('telefon')) {
        // Rakamlar ve olası boşluk, artı, tire dışında her şeyi sil
        const originalValue = e.target.value;
        const newValue = originalValue.replace(/[^0-9\s\+\-]/g, '');
        if (originalValue !== newValue) {
            e.target.value = newValue;
        }
    }

    // Sadece harf girilen yerde rakam engelleme (Örn: Ad, Soyad)
    if (nameAttr === 'name' || nameAttr === 'surname' || nameAttr === 'firstname' || nameAttr === 'lastname' || nameAttr === 'ad' || nameAttr === 'soyad') {
        const originalValue = e.target.value;
        const newValue = originalValue.replace(/[0-9]/g, '');
        if (originalValue !== newValue) {
            e.target.value = newValue;
        }
    }
});
