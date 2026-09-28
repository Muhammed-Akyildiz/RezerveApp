// RezerveApp — Super Admin shell interactions
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

