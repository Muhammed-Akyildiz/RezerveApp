// RezerveApp — Business Admin shell interactions
(function () {
    if (window.lucide) { lucide.createIcons(); }

    const sidebar = document.getElementById('sidebar');
    const backdrop = document.getElementById('sidebarBackdrop');
    const toggle = document.getElementById('menuToggle');

    function openSidebar() {
        sidebar?.classList.add('open');
        backdrop?.classList.add('open');
    }
    function closeSidebar() {
        sidebar?.classList.remove('open');
        backdrop?.classList.remove('open');
    }

    toggle?.addEventListener('click', () => {
        sidebar?.classList.contains('open') ? closeSidebar() : openSidebar();
    });
    backdrop?.addEventListener('click', closeSidebar);

    // Simple client-side table search/filter (progressive enhancement only —
    // replace with a server round-trip / AJAX call to your existing search action).
    const searchInput = document.querySelector('.toolbar input[name="q"]');
    const table = document.querySelector('.data-table');
    searchInput?.addEventListener('input', (e) => {
        const term = e.target.value.trim().toLowerCase();
        if (!table) return;
        table.querySelectorAll('tbody tr').forEach(row => {
            row.style.display = row.textContent.toLowerCase().includes(term) ? '' : 'none';
        });
    });
})();

// ==========================================================================
// Global SweetAlert2 Confirmation — Tek merkezden yönetilen silme uyarısı
// Kullanım: <form data-confirm="Mesaj metni"> veya <form data-confirm>
// data-confirm-title="Özel Başlık" (opsiyonel)
// ==========================================================================
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var title = form.getAttribute('data-confirm-title') || 'Emin misiniz?';
            var message = form.getAttribute('data-confirm') || 'Bu işlem geri alınamaz.';

            Swal.fire({
                title: title,
                html: '<div style="font-size:14px; color:#aaa;">' + message + '</div>',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Evet, Devam Et',
                cancelButtonText: 'Vazgeç',
                confirmButtonColor: '#EF4444',
                cancelButtonColor: '#252A34',
                background: '#151922',
                color: '#F5F5F5'
            }).then(function (result) {
                if (result.isConfirmed) {
                    // submit event'ini tekrar tetiklememek için doğrudan gönder
                    form.removeAttribute('data-confirm');
                    form.submit();
                }
            });
        });
    });
});
