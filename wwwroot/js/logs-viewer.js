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