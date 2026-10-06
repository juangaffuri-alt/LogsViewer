window.LogsViewer = {
    currentPage: 1,
    pageSize: 50,
    totalCount: 0,

    init: function (options) {
        if (options) {
            this.currentPage = options.currentPage || 1;
            this.pageSize = options.pageSize || 50;
            this.totalCount = options.totalCount || 0;
        }
        this.restoreViewMode();
        this.setupEventListeners();
    },

    setupEventListeners: function () {
        document.getElementById('btn-refresh')?.addEventListener('click', () => {
            window.location.reload();
        });

        this.startRelativeTimestamps();

        document.getElementById('btn-export')?.addEventListener('click', () => {
            this.exportCsv();
        });

        document.querySelectorAll('.view-toggle').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.setViewMode(e.currentTarget.dataset.view);
            });
        });

        document.querySelectorAll('.btn-expand').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const target = document.getElementById(e.currentTarget.dataset.target);
                if (!target) return;
                target.hidden = !target.hidden;
                e.currentTarget.classList.toggle('expanded', !target.hidden);
            });
        });
    },

    setViewMode: function (mode) {
        const grid = document.getElementById('logs-grid');
        const card = document.getElementById('logs-card');
        if (!grid || !card) return;

        grid.hidden = (mode === 'card');
        card.hidden = (mode !== 'card');

        document.querySelectorAll('.view-toggle').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.view === mode);
        });

        try { localStorage.setItem('logsViewMode', mode); } catch (e) { }
    },

    restoreViewMode: function () {
        let mode = 'grid';
        try { mode = localStorage.getItem('logsViewMode') || 'grid'; } catch (e) { }
        this.setViewMode(mode);
    },

    // Mantiene actualizados los timestamps relativos ("hace 3 min") de la grilla
    // y las tarjetas sin recargar la página. El valor absoluto queda en el title.
    startRelativeTimestamps: function () {
        const els = Array.from(document.querySelectorAll('.log-timestamp-relative[data-ts]'));
        if (!els.length) return;

        const fmt = (d) => {
            const s = Math.floor(d / 1000);
            if (s < 60) return `hace ${s} s`;
            const m = Math.floor(s / 60);
            if (m < 60) return `hace ${m} min`;
            const h = Math.floor(m / 60);
            if (h < 24) return `hace ${h} h`;
            return `hace ${Math.floor(h / 24)} d`;
        };

        const tick = () => {
            const now = Date.now();
            for (const el of els) {
                const ts = new Date(el.dataset.ts).getTime();
                if (isNaN(ts)) continue;
                const delta = now - ts;
                el.textContent = delta < 0 ? 'ahora' : fmt(delta);
            }
        };

        tick();
        setInterval(tick, 30000);
    },

    exportCsv: function () {
        const rows = [['Timestamp', 'Level', 'Source', 'Message', 'Exception']];
        document.querySelectorAll('#logs-grid tbody tr.log-row').forEach(tr => {
            const cells = tr.querySelectorAll('td');
            const exceptionRow = tr.nextElementSibling;
            const exceptionText = exceptionRow?.classList.contains('log-exception-row')
                ? exceptionRow.querySelector('pre')?.textContent || ''
                : '';

            rows.push([
                cells[1]?.textContent.trim() || '',
                cells[0]?.textContent.trim() || '',
                cells[2]?.textContent.trim() || '',
                cells[3]?.textContent.trim() || '',
                exceptionText
            ]);
        });

        const csv = rows
            .map(r => r.map(c => `"${String(c).replace(/"/g, '""')}"`).join(','))
            .join('\n');

        const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `logs-${new Date().toISOString().slice(0, 10)}.csv`;
        link.click();
        URL.revokeObjectURL(link.href);
    }
};