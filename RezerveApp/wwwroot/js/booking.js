// RezerveApp — Customer booking wizard
// ------------------------------------------------------------

(function () {

    if (window.lucide) {
        lucide.createIcons();
    }

    // Tarihi UTC'ye çevirmeden, yerel takvim günü olarak YYYY-MM-DD üretir.
    function formatLocalDateKey(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    // YYYY-MM-DD metnini UTC'ye çevirmeden yerel Date olarak oluşturur.
    function parseLocalDate(dateStr) {
        const [year, month, day] = dateStr.split('-').map(Number);
        return new Date(year, month - 1, day);
    }

    const state = {
        step: 1,
        serviceId: null,
        serviceName: null,
        servicePrice: 0,
        serviceDuration: null,
        employeeId: '',
        employeeName: 'Fark Etmez',
        employeeFlexible: true,
        date: null,
        dateLabel: null,
        time: null
    };

    const closedWeekday =
        (window.__bookingConfig &&
            window.__bookingConfig.closedWeekday) ?? null;


    // =========================================================
    // STEP NAVIGATION
    // =========================================================

    window.goToStep = function (step) {

        // ==========================================
        // VALIDATION BEFORE TRANSITION
        // ==========================================
        if (step === 6) {
            const nameEl = document.getElementById('CustomerName');
            const phoneEl = document.getElementById('CustomerPhone');
            
            if (nameEl && phoneEl) {
                const name = nameEl.value.trim();
                const phone = phoneEl.value.trim();
                let errorMsg = "";

                if (!name) {
                    errorMsg = "Lütfen adınızı ve soyadınızı girin.";
                } else if (!phone || phone.length !== 11) {
                    errorMsg = "Lütfen telefon numaranızı başında 0 olacak şekilde tam 11 haneli (örneğin 05320000000) girin.";
                }

                if (errorMsg) {
                    let errDiv = document.getElementById('formErrorMsg');
                    if (!errDiv) {
                        errDiv = document.createElement('div');
                        errDiv.id = 'formErrorMsg';
                        errDiv.style.color = '#e74c3c';
                        errDiv.style.backgroundColor = 'rgba(231, 76, 60, 0.1)';
                        errDiv.style.border = '1px solid rgba(231, 76, 60, 0.2)';
                        errDiv.style.padding = '10px 14px';
                        errDiv.style.borderRadius = '8px';
                        errDiv.style.marginBottom = '20px';
                        errDiv.style.fontSize = '14px';
                        
                        const actions = document.querySelector('[data-step-panel="5"] .wizard-actions');
                        actions.parentNode.insertBefore(errDiv, actions);
                    }
                    errDiv.innerText = errorMsg;
                    return; // DO NOT PROCEED TO STEP 6
                } else {
                    const errDiv = document.getElementById('formErrorMsg');
                    if (errDiv) errDiv.remove();
                    
                    // Beni Hatırla (LocalStorage'a kaydet)
                    const rememberMe = document.getElementById('RememberMe');
                    if (rememberMe && rememberMe.checked) {
                        localStorage.setItem('rezerveapp_customer_name', name);
                        localStorage.setItem('rezerveapp_customer_phone', phone);
                    } else {
                        localStorage.removeItem('rezerveapp_customer_name');
                        localStorage.removeItem('rezerveapp_customer_phone');
                    }
                }
            }
        }
        
        if (step === 2 && window.__bookingConfig && window.__bookingConfig.employeeServiceMap) {
            const map = window.__bookingConfig.employeeServiceMap;
            const selectedServices = state.serviceIds || [];
            
            document.querySelectorAll('.employee-pick-card').forEach(card => {
                const empId = card.dataset.id;
                
                // "Fark Etmez" card has empty empId
                if (!empId) {
                    card.style.display = '';
                    return;
                }
                
                // Check if this employee provides ALL selected services
                const employeeIdInt = parseInt(empId);
                let providesAll = true;
                
                for (let i = 0; i < selectedServices.length; i++) {
                    const sid = parseInt(selectedServices[i]);
                    const hasService = map.some(m => m.EmployeeId === employeeIdInt && m.ServiceId === sid);
                    if (!hasService) {
                        providesAll = false;
                        break;
                    }
                }
                
                if (providesAll) {
                    card.classList.remove('disabled');
                    card.style.opacity = '1';
                    card.style.filter = 'none';
                } else {
                    card.classList.add('disabled');
                    card.style.opacity = '0.5'; // Daha belirgin
                    card.style.filter = 'grayscale(1)'; // Gri yapalım
                }
            });
            
            // If the currently selected employee is now disabled, reset to 'Fark Etmez'
            const currentSelected = document.querySelector('.employee-pick-card.selected');
            if (currentSelected && currentSelected.classList.contains('disabled')) {
                // Select 'Fark Etmez' by default
                const anyCard = document.querySelector('.employee-pick-any');
                if (anyCard) {
                    // Manual selection to avoid infinite loop
                    document.querySelectorAll('.employee-pick-card').forEach(c => c.classList.remove('selected'));
                    anyCard.classList.add('selected');
                    state.employeeId = '';
                    state.employeeName = 'Fark Etmez';
                    state.employeeFlexible = true;
                }
            }
        }
        
        document.querySelectorAll('.wizard-step').forEach(el => {

            el.style.display =
                parseInt(el.dataset.stepPanel) === step
                    ? ''
                    : 'none';

        });


        document.querySelectorAll('.step-rail-item').forEach(el => {

            const s = parseInt(el.dataset.step);

            el.classList.toggle('active', s === step);
            el.classList.toggle('done', s < step);

        });


        document.querySelectorAll('.step-rail-mobile-item').forEach(el => {

            const s = parseInt(el.dataset.step);

            el.classList.toggle('active', s === step);
            el.classList.toggle('done', s < step);

        });


        state.step = step;


        if (
            step === 3 &&
            !document.getElementById('calendar').dataset.rendered
        ) {
            renderCalendar();
        }


        if (step === 6) {
            updateSummary();
        }


        const wizard = document.querySelector('.wizard');

        if (wizard) {

            window.scrollTo({
                top: wizard.offsetTop - 20,
                behavior: 'smooth'
            });

        }

    };


    // =========================================================
    // STEP 1 — SERVICE
    // =========================================================

    window.selectService = function (card) {

        card.classList.toggle('selected');

        let selectedCards = document.querySelectorAll('#serviceGrid .pick-card.selected');
        
        let ids = [];
        let names = [];
        let totalDuration = 0;
        let totalPrice = 0;
        
        selectedCards.forEach(c => {
            ids.push(c.dataset.id);
            names.push(c.dataset.name);
            totalDuration += parseInt(c.dataset.duration) || 0;
            totalPrice += parseFloat(c.dataset.price) || 0;
        });

        state.serviceId = ids.join(',');
        state.serviceName = names.join(', ');
        state.servicePrice = totalPrice;
        state.serviceDuration = totalDuration;

        document.getElementById('ServiceId').value = state.serviceId;

        document.getElementById('next1').disabled = (ids.length === 0);

    };


    // =========================================================
    // STEP 2 — EMPLOYEE
    // =========================================================

    window.selectEmployee = function (card) {
        if (card.classList.contains('disabled')) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: 'Hizmet Kapsamı Dışında',
                    text: 'Bu uzman, ilk adımda seçmiş olduğunuz hizmetleri sağlamamaktadır. Lütfen başka bir uzman seçin.',
                    confirmButtonColor: '#c9a24d',
                    confirmButtonText: 'Tamam'
                });
            } else {
                alert('Bu uzman, ilk adımda seçtiğiniz hizmetleri sağlamamaktadır.');
            }
            return;
        }

        document
            .querySelectorAll('#employeeGrid .employee-pick-card')
            .forEach(c => c.classList.remove('selected'));


        card.classList.add('selected');


        state.employeeId =
            card.dataset.id || '';

        state.employeeFlexible =
            !card.dataset.id;

        state.employeeName =
            card.dataset.name || 'Fark Etmez';


        document.getElementById('EmployeeId').value =
            state.employeeId;

        // Eğer takvim çizilmişse yeni berberin kapalılık günlerine göre yeniden çiz
        if (document.getElementById('calendar') && document.getElementById('calendar').dataset.rendered === 'true') {
            renderCalendar();
        }

    };


    // =========================================================
    // STEP 3 — CALENDAR
    // =========================================================

    function renderCalendar() {

        const calendar =
            document.getElementById('calendar');

        calendar.dataset.rendered = 'true';


        let viewDate = new Date();

        viewDate.setDate(1);


        function draw() {

            const monthNames = [
                'Ocak',
                'Şubat',
                'Mart',
                'Nisan',
                'Mayıs',
                'Haziran',
                'Temmuz',
                'Ağustos',
                'Eylül',
                'Ekim',
                'Kasım',
                'Aralık'
            ];


            const dowNames = [
                'Pt',
                'Sa',
                'Ça',
                'Pe',
                'Cu',
                'Ct',
                'Pz'
            ];


            const year =
                viewDate.getFullYear();

            const month =
                viewDate.getMonth();


            const firstDay =
                new Date(year, month, 1);


            const startOffset =
                (firstDay.getDay() + 6) % 7;


            const daysInMonth =
                new Date(year, month + 1, 0).getDate();


            const today = new Date();

            today.setHours(0, 0, 0, 0);


            let html = `
                <div class="calendar-head">

                    <button
                        type="button"
                        class="cal-nav-btn"
                        id="calPrev">

                        <svg
                            class="icon-sm"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor">

                            <polyline points="15 18 9 12 15 6"/>

                        </svg>

                    </button>


                    <strong>
                        ${monthNames[month]} ${year}
                    </strong>


                    <button
                        type="button"
                        class="cal-nav-btn"
                        id="calNext">

                        <svg
                            class="icon-sm"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor">

                            <polyline points="9 18 15 12 9 6"/>

                        </svg>

                    </button>

                </div>


                <div class="calendar-grid">

                    ${dowNames
                    .map(d => `<div class="cal-dow">${d}</div>`)
                    .join('')}

                    ${Array
                    .from({ length: startOffset })
                    .map(() => `<div class="cal-day empty"></div>`)
                    .join('')}

            `;


            for (let d = 1; d <= daysInMonth; d++) {

                const cellDate =
                    new Date(year, month, d);


                const isPast =
                    cellDate < today;


                const cellDayOfWeek = cellDate.getDay();
                const cellDateStr = formatLocalDateKey(cellDate);
                
                let isClosed = false;

                if (window.__bookingConfig) {
                    // İşletmenin kapalı olduğu gün mü?
                    if (window.__bookingConfig.businessClosedDays && window.__bookingConfig.businessClosedDays.includes(cellDayOfWeek)) {
                        isClosed = true;
                    }

                    // İşletme için tatil mi?
                    if (!isClosed && window.__bookingConfig.businessHolidays && window.__bookingConfig.businessHolidays.includes(cellDateStr)) {
                        isClosed = true;
                    }

                    // Seçilen berberin kapalı olduğu gün mü?
                    if (!isClosed && window.__bookingConfig.employeeClosedDays) {
                        if (state.employeeId && state.employeeId !== '') {
                            isClosed = window.__bookingConfig.employeeClosedDays.some(x => x.employeeId == state.employeeId && x.day === cellDayOfWeek);
                        } else if (state.employeeFlexible) {
                            // "Fark Etmez" seçildiyse, tüm berberler bugün kapalıysa günü kapat
                            const closedEmps = window.__bookingConfig.employeeClosedDays.filter(x => x.day === cellDayOfWeek);
                            const totalEmps = document.querySelectorAll('#employeeGrid .employee-pick-card[data-id]').length;
                            if (totalEmps > 0 && closedEmps.length >= totalEmps) {
                                isClosed = true;
                            }
                        }
                    }
                }

                const disabled =
                    isPast || isClosed;

                const maxDate = new Date();
                maxDate.setHours(0,0,0,0);
                maxDate.setMonth(maxDate.getMonth() + 2);
                
                const isTooFar = cellDate > maxDate;

                const finalDisabled = disabled || isTooFar;

                const isToday =
                    cellDate.getTime() ===
                    today.getTime();


                const cellDateKey = formatLocalDateKey(cellDate);

                const isSelected =
                    state.date === cellDateKey;


                html += `
                    <div
                        class="cal-day
                        ${finalDisabled ? 'disabled' : ''}
                        ${isToday ? 'today' : ''}
                        ${isSelected ? 'selected' : ''}"

                        data-date="${cellDateKey}"

                        ${finalDisabled
                        ? ''
                        : 'onclick="selectDate(this)"'}>

                        ${d}

                    </div>
                `;

            }


            html += `</div>`;


            calendar.innerHTML =
                html;


            document
                .getElementById('calPrev')
                .addEventListener(
                    'click',
                    () => {

                        viewDate.setMonth(
                            viewDate.getMonth() - 1
                        );

                        draw();

                    }
                );


            document
                .getElementById('calNext')
                .addEventListener(
                    'click',
                    () => {

                        viewDate.setMonth(
                            viewDate.getMonth() + 1
                        );

                        draw();

                    }
                );

        }


        draw();

    }


    window.selectDate = function (cell) {

        document
            .querySelectorAll('.cal-day')
            .forEach(c =>
                c.classList.remove('selected')
            );


        cell.classList.add('selected');


        state.date =
            cell.dataset.date;


        const d =
            parseLocalDate(state.date);


        state.dateLabel =
            d.toLocaleDateString(
                'tr-TR',
                {
                    day: 'numeric',
                    month: 'long',
                    year: 'numeric',
                    weekday: 'long'
                }
            );


        document.getElementById('Date').value =
            state.date;


        document.getElementById('next3').disabled =
            false;


        document.getElementById('timeStepSub').textContent =
            `${state.dateLabel} için müsait saatler`;


        if (state.employeeFlexible) {
            state.employeeId = '';
            document.getElementById('EmployeeId').value = '';
        }

        fetchAvailability(state.date);

    };


    // =========================================================
    // STEP 4 — REAL AVAILABILITY
    // =========================================================

    async function fetchAvailability(dateStr) {

        const container =
            document.getElementById('timeSlotsContainer');


        container.innerHTML = `
            <div class="loading-slots">
                Müsait saatler kontrol ediliyor...
            </div>
        `;


        if (!state.serviceId) {

            container.innerHTML = `
                <div class="empty-slots">
                    Önce bir hizmet seçmelisiniz.
                </div>
            `;

            return;
        }


        /*
         * Sayfanın adresinden slug'ı alıyoruz.
         *
         * Örnek:
         * /Booking/test3-berber
         *
         * slug = test3-berber
         */

        const pathParts =
            window.location.pathname
                .split('/')
                .filter(Boolean);


        const bookingIndex =
            pathParts.findIndex(
                x => x.toLowerCase() === 'booking'
            );


        let slug = '';


        if (bookingIndex !== -1 &&
            pathParts[bookingIndex + 1]) {

            slug =
                pathParts[bookingIndex + 1];

        }


        if (!slug) {

            container.innerHTML = `
                <div class="empty-slots">
                    İşletme bilgisi bulunamadı.
                </div>
            `;

            return;
        }


        /*
         * Controller endpoint:
         *
         * GET
         * /Bookings/{slug}/Availability
         */

        const url =
            `/Booking/${encodeURIComponent(slug)}/Availability` +
            `?serviceIds=${encodeURIComponent(state.serviceId)}` +
            `&employeeId=${encodeURIComponent(state.employeeFlexible ? 0 : (state.employeeId || 0))}` +
            `&date=${encodeURIComponent(dateStr)}`;


        try {

            const response =
                await fetch(url, {
                    method: 'GET',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });


            if (!response.ok) {

                throw new Error(
                    `Sunucu ${response.status} döndürdü.`
                );

            }


            const html =
                await response.text();


            container.innerHTML =
                html;


            if (window.lucide) {
                lucide.createIcons();
            }


            // Saat seçimini aktif et
            container
                .querySelectorAll('.slot-pill:not(.disabled)')
                .forEach(slot => {

                    slot.addEventListener(
                        'click',
                        function () {

                            selectSlot(this);

                        }
                    );

                });


            // Önceki saat seçimini temizle
            state.time = null;

            document.getElementById('Time').value = '';

            document.getElementById('next4').disabled = true;

        }
        catch (error) {

            console.error(
                'Availability hatası:',
                error
            );


            container.innerHTML = `
                <div class="empty-slots">
                    Müsait saatler yüklenirken bir hata oluştu.
                    <br>
                    <small>
                        Lütfen tarihi veya berberi tekrar seçin.
                    </small>
                </div>
            `;

        }

    }


    // =========================================================
    // STEP 4 — SELECT SLOT
    // =========================================================

    window.selectSlot = function (pill) {

        document
            .querySelectorAll('.slot-pill')
            .forEach(p =>
                p.classList.remove('selected')
            );

        pill.classList.add('selected');

        state.time = pill.dataset.time;

        document.getElementById('Time').value =
            state.time;

        // "Fark Etmez" seçildiyse Availability endpointi
        // bu saat için uygun bir berber belirler.
        const assignedEmployeeId =
            pill.dataset.employeeId || '';

        const assignedEmployeeName =
            pill.dataset.employeeName || '';

        if (assignedEmployeeId) {
            state.employeeId = assignedEmployeeId;
            document.getElementById('EmployeeId').value =
                assignedEmployeeId;

            // Kullanıcı "Fark Etmez" seçmişse özet ekranında
            // gerçek atanan berberi gösterebiliriz.
            if (state.employeeName === 'Fark Etmez' &&
                assignedEmployeeName) {
                state.employeeName = assignedEmployeeName;
            }
        }

        document.getElementById('next4').disabled =
            false;

    };


    // =========================================================
    // STEP 6 — SUMMARY
    // =========================================================

    window.updateSummary = function () {

        document.getElementById('sumService').textContent =
            state.serviceName
                ? `${state.serviceName} (${state.serviceDuration} dk)`
                : '—';


        document.getElementById('sumEmployee').textContent =
            state.employeeName ||
            'Fark Etmez';


        document.getElementById('sumDateTime').textContent =
            (
                state.dateLabel &&
                state.time
            )
                ? `${state.dateLabel} · ${state.time}`
                : '—';


        document.getElementById('sumName').textContent =
            document.getElementById('CustomerName')?.value ||
            '—';


        document.getElementById('sumPhone').textContent =
            document.getElementById('CustomerPhone')?.value ||
            '—';


        document.getElementById('sumTotal').textContent =
            `₺${state.servicePrice || 0}`;

    };

    // =========================================================
    // AKILLI TAKVİM - İlk Müsait Saati Bul
    // =========================================================
    window.findNextAvailable = async function(btn) {
        btn.disabled = true;
        const originalText = btn.innerHTML;
        btn.innerHTML = 'Aranıyor... <span style="display:inline-block; animation: spin 1s linear infinite;">⏳</span>';
        
        let foundDateStr = null;
        
        const pathParts = window.location.pathname.split('/').filter(Boolean);
        const bookingIndex = pathParts.findIndex(x => x.toLowerCase() === 'booking');
        let slug = '';
        if (bookingIndex !== -1 && pathParts[bookingIndex + 1]) {
            slug = pathParts[bookingIndex + 1];
        }

        // 14 günü paralel sorgula
        const fetchPromises = [];
        for (let i = 0; i < 14; i++) {
            let checkDate = new Date();
            checkDate.setDate(checkDate.getDate() + i);
            let dateStr = formatLocalDateKey(checkDate);
            const url = `/Booking/${encodeURIComponent(slug)}/Availability?serviceIds=${encodeURIComponent(state.serviceId)}&employeeId=${encodeURIComponent(state.employeeFlexible ? 0 : (state.employeeId || 0))}&date=${encodeURIComponent(dateStr)}`;
            
            fetchPromises.push(
                fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                    .then(r => r.ok ? r.text() : '')
                    .then(html => ({ dateStr, html }))
                    .catch(() => ({ dateStr, html: '' }))
            );
        }

        const results = await Promise.all(fetchPromises);

        for (const res of results) {
            const html = res.html;
            if (html.includes('slot-pill') && !html.includes('Kapalı') && !html.includes('hizmet vermiyor') && !html.includes('izinli')) {
                if (html.split('slot-pill').length > html.split('disabled').length) {
                    foundDateStr = res.dateStr;
                    break;
                }
            }
        }
        
        btn.innerHTML = originalText;
        btn.disabled = false;
        
        if (foundDateStr) {
            // Tarihi takvimde seçili hale getir
            state.date = foundDateStr;
            const d = parseLocalDate(state.date);
            state.dateLabel = d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' });
            document.getElementById('Date').value = state.date;
            document.getElementById('next3').disabled = false;
            document.getElementById('timeStepSub').textContent = `${state.dateLabel} için müsait saatler`;
            
            // Eğer esnekse sıfırla
            if (state.employeeFlexible) {
                state.employeeId = '';
                document.getElementById('EmployeeId').value = '';
            }
            
            // Sonraki adıma geçip saatleri göster
            goToStep(4);
            fetchAvailability(state.date);
        } else {
            Swal.fire({
                icon: 'warning',
                title: 'Uyarı',
                text: 'Önümüzdeki 14 gün içinde uygun boş saat bulunamadı.',
                confirmButtonColor: '#d4af37',
                confirmButtonText: 'Tamam',
                background: '#1a1a24',
                color: '#fff'
            });
        }
    };

    // BENİ HATIRLA - Sayfa yüklendiğinde LocalStorage'dan bilgileri çek
    document.addEventListener("DOMContentLoaded", function () {
        const savedName = localStorage.getItem('rezerveapp_customer_name');
        const savedPhone = localStorage.getItem('rezerveapp_customer_phone');
        
        const nameEl = document.getElementById('CustomerName');
        const phoneEl = document.getElementById('CustomerPhone');
        
        if (savedName && nameEl) nameEl.value = savedName;
        if (savedPhone && phoneEl) phoneEl.value = savedPhone;
    });


})();
