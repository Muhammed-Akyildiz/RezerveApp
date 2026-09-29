document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // İL / İLÇE
    // ==========================================

    if (typeof window.initAddressCascade === "function") {
        window.initAddressCascade(
            "business-city",
            "business-district",
            null,
            null
        );
    }


    // ==========================================
    // LOGO YÜKLEME
    // ==========================================

    const logoInput = document.getElementById("logo");
    const logoUpload = document.getElementById("logoUpload");

    if (logoInput && logoUpload) {

        logoUpload.addEventListener("click", function () {
            logoInput.click();
        });

        logoInput.addEventListener("change", function () {

            const file = logoInput.files[0];

            if (!file) return;

            // 10 MB kontrolü
            if (file.size > 10 * 1024 * 1024) {

                alert("Logo dosyası 10 MB'dan büyük olamaz.");

                logoInput.value = "";

                return;
            }

            // Dosya tipi kontrolü
            const allowedTypes = [
                "image/png",
                "image/jpeg",
                "image/webp",
                "image/heic",
                "image/heif"
            ];

            if (!allowedTypes.includes(file.type) && !file.name.toLowerCase().endsWith(".heic") && !file.name.toLowerCase().endsWith(".heif")) {

                alert("Sadece PNG, JPG, WEBP veya HEIC dosyaları yükleyebilirsiniz.");

                logoInput.value = "";

                return;
            }

            // Önizleme
            const reader = new FileReader();

            reader.onload = function (e) {

                logoUpload.innerHTML = `
                    <img
                        src="${e.target.result}"
                        alt="Logo önizleme"
                        style="
                            max-width: 140px;
                            max-height: 100px;
                            object-fit: contain;
                            border-radius: 10px;
                        "
                    >
                    <div style="margin-top: 8px;">
                        ${file.name}
                    </div>
                `;
            };

            reader.readAsDataURL(file);
        });
    }


    // ==========================================
    // LEAFLET HARİTA
    // ==========================================

    const mapElement = document.getElementById("businessMap");

    if (!mapElement || typeof L === "undefined") {
        console.warn("Harita başlatılamadı.");
        return;
    }

    // Türkiye merkezli başlangıç
    const defaultLatitude = 39.0;
    const defaultLongitude = 35.0;
    const defaultZoom = 6;

    const map = L.map("businessMap").setView(
        [defaultLatitude, defaultLongitude],
        defaultZoom
    );

    // OpenStreetMap
    L.tileLayer(
        "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        {
            attribution: "&copy; OpenStreetMap contributors"
        }
    ).addTo(map);


    let marker = null;


    // ==========================================
    // HARİTAYA TIKLAMA
    // ==========================================

    map.on("click", function (e) {

        const latitude = e.latlng.lat;
        const longitude = e.latlng.lng;

        // Eski marker varsa kaldır
        if (marker) {
            map.removeLayer(marker);
        }

        // Yeni marker
        marker = L.marker([
            latitude,
            longitude
        ]).addTo(map);

        marker.bindPopup(
            "İşletme konumu"
        ).openPopup();


        // Hidden inputlara yaz
        const latitudeInput =
            document.getElementById("latitude");

        const longitudeInput =
            document.getElementById("longitude");

        if (latitudeInput) {
            latitudeInput.value = latitude;
        }

        if (longitudeInput) {
            longitudeInput.value = longitude;
        }


        // Kullanıcıya göster
        const mapCoords =
            document.getElementById("mapCoords");

        if (mapCoords) {

            mapCoords.textContent =
                `Konum seçildi: ${latitude.toFixed(6)}, ${longitude.toFixed(6)}`;

        }
    });


    // ==========================================
    // HARİTA BOYUTUNU YENİLE
    // ==========================================

    setTimeout(function () {
        map.invalidateSize();
    }, 200);

});