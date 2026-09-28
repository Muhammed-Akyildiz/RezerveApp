(function () {
    'use strict';

    const root = document.getElementById('calendarRoot');
    if (!root) return;

    const employees = JSON.parse(root.dataset.employees || '[]');
    const eventsUrl = root.dataset.eventsUrl;
    const moveUrl = root.dataset.moveUrl;

    const tokenInput = root.querySelector(
        'input[name="__RequestVerificationToken"]'
    );

    const token = tokenInput ? tokenInput.value : '';

    const calBody = document.getElementById('calBody');
    const calTitle = document.getElementById('calTitle');
    const calMessage = document.getElementById('calMessage');

    const DAY_NAMES = [
        'Pazar',
        'Pazartesi',
        'Salı',
        'Çarşamba',
        'Perşembe',
        'Cuma',
        'Cumartesi'
    ];

    const DAY_NAMES_SHORT = [
        'Paz',
        'Pzt',
        'Sal',
        'Çar',
        'Per',
        'Cum',
        'Cmt'
    ];

    const MONTH_NAMES = [
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

    const PALETTE = [
        '#C9A227',
        '#4F8AE0',
        '#E06A4F',
        '#5FBF7A',
        '#B478DB',
        '#E0B84F',
        '#4FC1C6'
    ];

    const state = {
        view: root.dataset.initialView || 'day',
        date: parseDateOnly(root.dataset.initialDate) || new Date(),
        events: []
    };

    // =========================================================
    // TARİH YARDIMCILARI
    // =========================================================

    function parseDateOnly(str) {
        if (!str) return null;

        const parts = str.split('-').map(Number);

        if (parts.length !== 3) {
            return null;
        }

        return new Date(
            parts[0],
            parts[1] - 1,
            parts[2]
        );
    }

    function fmtDate(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');

        return `${y}-${m}-${day}`;
    }

    function addDays(d, n) {
        const copy = new Date(d);
        copy.setDate(copy.getDate() + n);
        return copy;
    }

    function startOfWeek(d) {
        // Pazartesi başlangıçlı hafta
        const day = (d.getDay() + 6) % 7;

        return addDays(d, -day);
    }

    function isSameDay(a, b) {
        return (
            a.getFullYear() === b.getFullYear() &&
            a.getMonth() === b.getMonth() &&
            a.getDate() === b.getDate()
        );
    }

    function timeToMinutes(t) {
        if (!t) return 0;

        const parts = t.split(':').map(Number);

        return parts[0] * 60 + parts[1];
    }

    function minutesToLabel(totalMinutes) {
        const h = Math.floor(totalMinutes / 60);
        const m = totalMinutes % 60;

        return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
    }

    // =========================================================
    // ÇALIŞAN RENGİ
    // =========================================================

    function employeeColor(employeeId) {
        const idx = employees.findIndex(
            e => e.id === employeeId
        );

        return PALETTE[
            (idx >= 0 ? idx : employeeId) % PALETTE.length
        ];
    }

    // =========================================================
    // RANDEVU DURUM SINIFI
    // =========================================================

    function getEventStateClass(ev) {
        const today = new Date();
        const eventDate = parseDateOnly(ev.date);

        if (!eventDate) {
            return '';
        }

        // İptal
        if (
            ev.status === 'Cancelled' ||
            ev.status === 'Rejected'
        ) {
            return 'is-cancelled';
        }

        // Tamamlandı
        if (ev.status === 'Completed') {
            return 'is-completed';
        }

        // Sadece tarih kısmı
        const todayOnly = new Date(
            today.getFullYear(),
            today.getMonth(),
            today.getDate()
        );

        // Geçmiş gün
        if (eventDate < todayOnly) {
            return 'is-past';
        }

        // Bugün
        if (isSameDay(eventDate, today)) {
            const nowMinutes =
                today.getHours() * 60 +
                today.getMinutes();

            const endMinutes =
                timeToMinutes(ev.endTime);

            // Randevunun saati geçmişse
            if (endMinutes <= nowMinutes) {
                return 'is-past';
            }

            // Bugünün yaklaşan randevusu
            return 'is-today';
        }

        // Gelecek gün
        return '';
    }

    // =========================================================
    // MESAJ
    // =========================================================

    function showMessage(text, isSuccess) {
        calMessage.textContent = text;

        calMessage.className =
            'cal-message' +
            (isSuccess ? ' success' : '');

        calMessage.style.display = 'block';

        clearTimeout(showMessage._t);

        showMessage._t = setTimeout(() => {
            calMessage.style.display = 'none';
        }, 4000);
    }

    // =========================================================
    // VERİ ÇEKME
    // =========================================================

    function getVisibleRange() {
        if (state.view === 'day') {
            return {
                start: state.date,
                end: state.date
            };
        }

        if (state.view === 'week') {
            const s = startOfWeek(state.date);

            return {
                start: s,
                end: addDays(s, 6)
            };
        }

        // Ay görünümü
        const first = new Date(
            state.date.getFullYear(),
            state.date.getMonth(),
            1
        );

        const last = new Date(
            state.date.getFullYear(),
            state.date.getMonth() + 1,
            0
        );

        return {
            start: startOfWeek(first),
            end: addDays(startOfWeek(last), 6)
        };
    }

    async function loadEvents() {
        const { start, end } = getVisibleRange();

        const url =
            `${eventsUrl}?start=${fmtDate(start)}&end=${fmtDate(end)}`;

        calBody.innerHTML =
            '<div class="cal-loading">Takvim yükleniyor...</div>';

        try {
            const res = await fetch(url, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!res.ok) {
                throw new Error(
                    `Sunucu hatası: ${res.status}`
                );
            }

            state.events = await res.json();

        } catch (e) {
            console.error(
                'Takvim verileri alınamadı:',
                e
            );

            calBody.innerHTML = `
                <div class="cal-loading">
                    <strong>Takvim yüklenemedi.</strong>
                    <br>
                    Lütfen sayfayı yenileyin.
                </div>
            `;

            return;
        }

        render();
    }

    // =========================================================
    // GENEL RENDER
    // =========================================================

    function updateTitle() {
        if (state.view === 'day') {

            calTitle.textContent =
                `${DAY_NAMES[state.date.getDay()]}, ` +
                `${state.date.getDate()} ` +
                `${MONTH_NAMES[state.date.getMonth()]} ` +
                `${state.date.getFullYear()}`;

        } else if (state.view === 'week') {

            const s = startOfWeek(state.date);
            const e = addDays(s, 6);

            calTitle.textContent =
                `${s.getDate()} ${MONTH_NAMES[s.getMonth()]} ` +
                `— ` +
                `${e.getDate()} ${MONTH_NAMES[e.getMonth()]} ` +
                `${e.getFullYear()}`;

        } else {

            calTitle.textContent =
                `${MONTH_NAMES[state.date.getMonth()]} ` +
                `${state.date.getFullYear()}`;
        }
    }

    function render() {
        updateTitle();

        document
            .querySelectorAll('.cal-view-btn')
            .forEach(b => {
                b.classList.toggle(
                    'active',
                    b.dataset.view === state.view
                );
            });

        if (state.view === 'day') {
            renderDay();
        }
        else if (state.view === 'week') {
            renderWeek();
        }
        else {
            renderMonth();
        }
    }

    // =========================================================
    // GÜN GÖRÜNÜMÜ
    // =========================================================

    const DAY_START_MIN = 8 * 60;
    const DAY_END_MIN = 22 * 60;
    const SLOT_MIN = 30;
    const ROW_HEIGHT = 48;

    function renderDay() {

        const dayEvents =
            state.events.filter(
                ev => ev.date === fmtDate(state.date)
            );

        const cols =
            employees.length > 0
                ? employees
                : [
                    {
                        id: 0,
                        name: 'Çalışan yok'
                    }
                ];

        let html =
            '<div class="cal-day-grid">';

        // Header
        html += `
            <div
                class="cal-day-header"
                style="grid-template-columns: 64px repeat(${cols.length}, 1fr);"
            >
        `;

        html +=
            '<div class="cal-day-header-cell cal-time-col-header"></div>';

        cols.forEach(emp => {

            html += `
                <div class="cal-day-header-cell">
                    ${escapeHtml(emp.name)}
                </div>
            `;
        });

        html += '</div>';

        // Satırlar
        const rowCount =
            (DAY_END_MIN - DAY_START_MIN) /
            SLOT_MIN;

        html += `
            <div
                class="cal-day-rows"
                style="
                    grid-template-columns:
                        64px repeat(${cols.length}, 1fr);
                    height:${rowCount * ROW_HEIGHT}px;
                "
            >
        `;

        for (
            let r = 0;
            r < rowCount;
            r++
        ) {

            const minutes =
                DAY_START_MIN +
                r * SLOT_MIN;

            const label =
                minutesToLabel(minutes);

            html += `
                <div
                    class="cal-time-label"
                    style="
                        grid-column:1;
                        grid-row:${r + 1};
                    "
                >
                    ${minutes % 60 === 0 ? label : ''}
                </div>
            `;

            cols.forEach((emp, colIdx) => {

                html += `
                    <div
                        class="cal-slot-cell"
                        style="
                            grid-column:${colIdx + 2};
                            grid-row:${r + 1};
                        "
                        data-employee-id="${emp.id}"
                        data-time="${label}"
                    ></div>
                `;
            });
        }

        html += '</div></div>';

        calBody.innerHTML = html;

        const rowsContainer =
            calBody.querySelector(
                '.cal-day-rows'
            );

        // Randevular
        cols.forEach((emp, colIdx) => {

            const empEvents =
                dayEvents.filter(
                    ev => ev.employeeId === emp.id
                );

            empEvents.forEach(ev => {

                const startMin =
                    timeToMinutes(
                        ev.startTime
                    );

                const endMin =
                    timeToMinutes(
                        ev.endTime
                    );

                const top =
                    ((startMin - DAY_START_MIN) /
                        SLOT_MIN) *
                    ROW_HEIGHT;

                const height =
                    Math.max(
                        ((endMin - startMin) /
                            SLOT_MIN) *
                        ROW_HEIGHT -
                        2,
                        20
                    );

                const el =
                    document.createElement('div');

                // ÖNEMLİ:
                // Eski status sınıfı + yeni durum sınıfı
                el.className =
                    'cal-event status-' +
                    String(
                        ev.status || ''
                    ).toLowerCase() +
                    ' ' +
                    getEventStateClass(ev);

                el.style.background =
                    employeeColor(emp.id);

                el.style.top =
                    top + 'px';

                el.style.height =
                    height + 'px';

                el.style.left =
                    `calc(64px + ${colIdx} * ` +
                    `(100% - 64px) / ${cols.length} + 4px)`;

                el.style.width =
                    `calc((100% - 64px) / ` +
                    `${cols.length} - 8px)`;

                el.style.position =
                    'absolute';

                el.dataset.id =
                    ev.id;

                el.dataset.rowVersion =
                    ev.rowVersion || '';

                el.draggable = true;

                el.innerHTML = `
                    <div class="cal-event-name">
                        ${escapeHtml(ev.customerName)}
                    </div>
                    <div class="cal-event-service">
                        ${escapeHtml(ev.serviceName)} · ${ev.startTime}
                    </div>
                    <div class="cal-event-employee" style="font-size: 10.5px; opacity: 0.9; margin-top: 3px; font-weight: 500; letter-spacing: 0.02em;">
                        <svg class="icon-sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" style="width: 11px; height: 11px; margin-right: 2px; vertical-align: text-top;"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                        ${escapeHtml(emp.name)}
                    </div>
                `;

                attachDragEvents(
                    el,
                    ev
                );

                rowsContainer.appendChild(
                    el
                );
            });
        });

        attachDropTargets(
            calBody.querySelectorAll(
                '.cal-slot-cell'
            ),
            cell => ({
                date: state.date,
                time: cell.dataset.time,
                employeeId:
                    parseInt(
                        cell.dataset.employeeId,
                        10
                    )
            })
        );
    }

    // =========================================================
    // HAFTA GÖRÜNÜMÜ
    // =========================================================

    function renderWeek() {

        const s =
            startOfWeek(state.date);

        const days =
            Array.from(
                { length: 7 },
                (_, i) =>
                    addDays(s, i)
            );

        const today =
            new Date();

        let html =
            '<div class="cal-week-grid">';

        days.forEach(d => {

            const isToday =
                isSameDay(
                    d,
                    today
                );

            html += `
                <div class="cal-week-day">

                    <div
                        class="
                            cal-week-day-head
                            ${isToday ? 'is-today' : ''}
                        "
                    >
                        ${DAY_NAMES_SHORT[d.getDay()]}
                        <br>
                        ${d.getDate()}
                        ${MONTH_NAMES[d.getMonth()].substring(0, 3)}
                    </div>

                    <div
                        class="cal-week-day-body"
                        data-date="${fmtDate(d)}"
                    ></div>

                </div>
            `;
        });

        html += '</div>';

        calBody.innerHTML = html;

        days.forEach(d => {

            const container =
                calBody.querySelector(
                    `.cal-week-day-body[data-date="${fmtDate(d)}"]`
                );

            const dayEvents =
                state.events
                    .filter(
                        ev =>
                            ev.date ===
                            fmtDate(d)
                    )
                    .sort(
                        (a, b) =>
                            a.startTime.localeCompare(
                                b.startTime
                            )
                    );

            dayEvents.forEach(ev => {

                const chip =
                    document.createElement(
                        'div'
                    );

                chip.className =
                    'cal-week-chip ' +
                    getEventStateClass(ev);

                chip.style.background =
                    employeeColor(
                        ev.employeeId
                    );

                chip.draggable = true;

                chip.dataset.id =
                    ev.id;

                chip.dataset.rowVersion =
                    ev.rowVersion || '';

                chip.innerHTML = `
                    <strong>
                        ${ev.startTime}
                    </strong>

                    ${escapeHtml(
                    ev.customerName
                )}

                    <br>

                    <span style="opacity:.8;">
                        ${escapeHtml(
                    ev.employeeName
                )}
                        ·
                        ${escapeHtml(
                    ev.serviceName
                )}
                    </span>
                `;

                attachDragEvents(
                    chip,
                    ev
                );

                container.appendChild(
                    chip
                );
            });
        });

        attachDropTargets(
            calBody.querySelectorAll(
                '.cal-week-day-body'
            ),
            cell => {

                const [
                    y,
                    m,
                    dd
                ] =
                    cell.dataset.date
                        .split('-')
                        .map(Number);

                return {
                    date:
                        new Date(
                            y,
                            m - 1,
                            dd
                        ),

                    time: null,

                    employeeId: null
                };
            }
        );
    }

    // =========================================================
    // AY GÖRÜNÜMÜ
    // =========================================================

    function renderMonth() {

        const first =
            new Date(
                state.date.getFullYear(),
                state.date.getMonth(),
                1
            );

        const gridStart =
            startOfWeek(first);

        const today =
            new Date();

        let html =
            '<div class="cal-month-grid">';

        // Haftanın günleri
        DAY_NAMES_SHORT.forEach((_, i) => {

            const idx =
                (i + 1) % 7;

            html += `
                <div class="cal-month-weekday">
                    ${DAY_NAMES_SHORT[idx]}
                </div>
            `;
        });

        // 42 hücre
        const cells =
            Array.from(
                { length: 42 },
                (_, i) =>
                    addDays(
                        gridStart,
                        i
                    )
            );

        cells.forEach(d => {

            const isOtherMonth =
                d.getMonth() !==
                state.date.getMonth();

            const isToday =
                isSameDay(
                    d,
                    today
                );

            html += `
                <div
                    class="
                        cal-month-cell
                        ${isOtherMonth ? 'other-month' : ''}
                    "
                    data-date="${fmtDate(d)}"
                >

                    <div
                        class="
                            cal-month-day-num
                            ${isToday ? 'is-today' : ''}
                        "
                        data-goto="${fmtDate(d)}"
                    >
                        ${d.getDate()}
                    </div>

                    <div
                        class="cal-month-chips"
                        data-date-chips="${fmtDate(d)}"
                    ></div>

                </div>
            `;
        });

        html += '</div>';

        calBody.innerHTML = html;

        cells.forEach(d => {

            const key =
                fmtDate(d);

            const container =
                calBody.querySelector(
                    `.cal-month-chips[data-date-chips="${key}"]`
                );

            const dayEvents =
                state.events
                    .filter(
                        ev =>
                            ev.date ===
                            key
                    )
                    .sort(
                        (a, b) =>
                            a.startTime.localeCompare(
                                b.startTime
                            )
                    );

            const maxShow = 3;

            dayEvents
                .slice(0, maxShow)
                .forEach(ev => {

                    const chip =
                        document.createElement(
                            'div'
                        );

                    chip.className =
                        'cal-month-chip ' +
                        getEventStateClass(ev);

                    chip.style.background =
                        employeeColor(
                            ev.employeeId
                        );

                    chip.draggable =
                        true;

                    chip.dataset.id =
                        ev.id;

                    chip.dataset.rowVersion =
                        ev.rowVersion || '';

                    chip.title =
                        `${ev.startTime} ` +
                        `${ev.customerName} — ` +
                        `${ev.employeeName}`;

                    chip.textContent =
                        `${ev.startTime} ` +
                        `${ev.customerName} - ${ev.employeeName}`;

                    attachDragEvents(
                        chip,
                        ev
                    );

                    container.appendChild(
                        chip
                    );
                });

            if (
                dayEvents.length >
                maxShow
            ) {

                const more =
                    document.createElement(
                        'div'
                    );

                more.className =
                    'cal-month-more';

                more.textContent =
                    `+${dayEvents.length - maxShow} daha`;

                container.appendChild(
                    more
                );
            }
        });

        // Ay görünümünde güne tıklayınca gün görünümü
        calBody
            .querySelectorAll(
                '.cal-month-day-num'
            )
            .forEach(el => {

                el.addEventListener(
                    'click',
                    () => {

                        state.date =
                            parseDateOnly(
                                el.dataset.goto
                            );

                        state.view =
                            'day';

                        loadEvents();
                    }
                );
            });

        attachDropTargets(
            calBody.querySelectorAll(
                '.cal-month-cell'
            ),
            cell => {

                const [
                    y,
                    m,
                    dd
                ] =
                    cell.dataset.date
                        .split('-')
                        .map(Number);

                return {
                    date:
                        new Date(
                            y,
                            m - 1,
                            dd
                        ),

                    time: null,

                    employeeId: null
                };
            }
        );
    }

    // =========================================================
    // SÜRÜKLE - BIRAK
    // =========================================================

    let draggedEvent = null;

    function attachDragEvents(
        el,
        ev
    ) {

        el.addEventListener(
            'dragstart',
            () => {

                draggedEvent =
                    ev;

                el.classList.add(
                    'dragging'
                );
            }
        );

        el.addEventListener(
            'dragend',
            () => {

                el.classList.remove(
                    'dragging'
                );

                draggedEvent =
                    null;
            }
        );
    }

    function attachDropTargets(
        cells,
        resolveTarget
    ) {

        cells.forEach(cell => {

            cell.addEventListener(
                'dragover',
                e => {

                    e.preventDefault();

                    cell.classList.add(
                        'drag-over'
                    );
                }
            );

            cell.addEventListener(
                'dragleave',
                () => {

                    cell.classList.remove(
                        'drag-over'
                    );
                }
            );

            cell.addEventListener(
                'drop',
                async e => {

                    e.preventDefault();

                    cell.classList.remove(
                        'drag-over'
                    );

                    if (!draggedEvent) {
                        return;
                    }

                    const target =
                        resolveTarget(
                            cell
                        );

                    const newDate =
                        target.date;

                    const newTime =
                        target.time ||
                        draggedEvent.startTime;

                    const newEmployeeId =
                        target.employeeId != null
                            ? target.employeeId
                            : draggedEvent.employeeId;

                    await moveAppointment(
                        draggedEvent,
                        newDate,
                        newTime,
                        newEmployeeId
                    );
                }
            );
        });
    }

    // =========================================================
    // RANDEVU TAŞIMA
    // =========================================================

    async function moveAppointment(
        ev,
        newDate,
        newTime,
        newEmployeeId
    ) {

        const body =
            new URLSearchParams();

        body.set(
            'id',
            ev.id
        );

        body.set(
            'newDate',
            fmtDate(newDate)
        );

        body.set(
            'newStartTime',
            newTime
        );

        body.set(
            'newEmployeeId',
            newEmployeeId
        );

        body.set(
            'rowVersion',
            ev.rowVersion || ''
        );

        body.set(
            '__RequestVerificationToken',
            token
        );

        try {

            const res =
                await fetch(
                    moveUrl,
                    {
                        method: 'POST',

                        headers: {
                            'Content-Type':
                                'application/x-www-form-urlencoded'
                        },

                        body:
                            body.toString()
                    }
                );

            const data =
                await res.json();

            if (data.success) {

                showMessage(
                    'Randevu güncellendi.',
                    true
                );

                await loadEvents();

            } else {

                showMessage(
                    data.message ||
                    'Randevu taşınamadı.',
                    false
                );
            }

        } catch (e) {

            console.error(
                'Randevu taşıma hatası:',
                e
            );

            showMessage(
                'Sunucuyla iletişim kurulamadı.',
                false
            );
        }
    }

    // =========================================================
    // HTML GÜVENLİĞİ
    // =========================================================

    function escapeHtml(str) {

        const div =
            document.createElement(
                'div'
            );

        div.textContent =
            str || '';

        return div.innerHTML;
    }

    // =========================================================
    // TOOLBAR
    // =========================================================

    document
        .getElementById('calPrev')
        .addEventListener(
            'click',
            () => {

                if (
                    state.view === 'day'
                ) {

                    state.date =
                        addDays(
                            state.date,
                            -1
                        );

                } else if (
                    state.view === 'week'
                ) {

                    state.date =
                        addDays(
                            state.date,
                            -7
                        );

                } else {

                    state.date =
                        new Date(
                            state.date.getFullYear(),
                            state.date.getMonth() - 1,
                            1
                        );
                }

                loadEvents();
            }
        );

    document
        .getElementById('calNext')
        .addEventListener(
            'click',
            () => {

                if (
                    state.view === 'day'
                ) {

                    state.date =
                        addDays(
                            state.date,
                            1
                        );

                } else if (
                    state.view === 'week'
                ) {

                    state.date =
                        addDays(
                            state.date,
                            7
                        );

                } else {

                    state.date =
                        new Date(
                            state.date.getFullYear(),
                            state.date.getMonth() + 1,
                            1
                        );
                }

                loadEvents();
            }
        );

    document
        .getElementById('calToday')
        .addEventListener(
            'click',
            () => {

                state.date =
                    new Date();

                loadEvents();
            }
        );

    document
        .querySelectorAll(
            '.cal-view-btn'
        )
        .forEach(btn => {

            btn.addEventListener(
                'click',
                () => {

                    state.view =
                        btn.dataset.view;

                    loadEvents();
                }
            );
        });

    // =========================================================
    // BAŞLAT
    // =========================================================

    loadEvents();

})();