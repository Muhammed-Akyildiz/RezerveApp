// Şehir seçilince ilçe listesini dolduran basit cascade-select yardımcı.
// window.TR_IL_ILCE (wwwroot/js/data/tr-il-ilce.js) veri kaynağıdır.
window.initAddressCascade = function (citySelectId, districtSelectId, initialCity, initialDistrict) {
    var citySelect = document.getElementById(citySelectId);
    var districtSelect = document.getElementById(districtSelectId);

    if (!citySelect || !districtSelect || !window.TR_IL_ILCE) return;

    var cities = Object.keys(window.TR_IL_ILCE).sort(function (a, b) { return a.localeCompare(b, 'tr'); });

    citySelect.innerHTML = '<option value="">Şehir seçiniz</option>' +
        cities.map(function (c) { return '<option value="' + c + '">' + c + '</option>'; }).join('');

    function populateDistricts(city, selectedDistrict) {
        var districts = (window.TR_IL_ILCE[city] || []).slice();

        if (districts.length === 0) {
            districtSelect.innerHTML = '<option value="">Önce şehir seçin</option>';
            districtSelect.disabled = true;
            return;
        }

        districtSelect.innerHTML = '<option value="">İlçe seçiniz</option>' +
            districts.map(function (d) {
                var sel = (d === selectedDistrict) ? ' selected' : '';
                return '<option value="' + d + '"' + sel + '>' + d + '</option>';
            }).join('');

        districtSelect.disabled = false;
    }

    citySelect.addEventListener('change', function () {
        populateDistricts(citySelect.value, null);
    });

    if (initialCity && window.TR_IL_ILCE[initialCity]) {
        citySelect.value = initialCity;
        populateDistricts(initialCity, initialDistrict);
    } else {
        districtSelect.innerHTML = '<option value="">Önce şehir seçin</option>';
        districtSelect.disabled = true;
    }
};
