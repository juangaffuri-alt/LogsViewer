window.LogsViewer = {
    currentPage: 1,
    pageSize: 50,
    filters: {},

    init: function () {
        this.loadLogs();
        this.setupEventListeners();
    },

    async loadLogs() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...this.filters
            });

            const response = await fetch(`/api/logs/search?${params}`);
            const data = await response.json();

            this.renderLogs(data.logs);
            this.renderPagination(data.totalPages);
        } catch (error) {
            console.error('Error loading logs:', error);
        }
    },

    renderLogs: function (logs) {
        const container = document.getElementById('logs-list');
        container.innerHTML = logs.map(log => `
            <div class="log-card">
                <div class="log-header">
                    <span class="log-level ${log.level.toLowerCase()}">
                        ${log.level}
                    </span>
                    <span class="log-timestamp">${new Date(log.timestamp).toLocaleString()}</span>
                    <span class="log-source">${log.source}</span>
                </div>
                <div class="log-message">${log.message}</div>
                ${log.properties ? `
                    <details class="log-details">
                        <summary>Propiedades</summary>
                        <pre>${JSON.stringify(log.properties, null, 2)}</pre>
                    </details>
                ` : ''}
            </div>
        `).join('');
    },

    renderPagination: function (totalPages) {
        const container = document.getElementById('pagination-container');
        let html = '';

        // Botón anterior
        if (this.currentPage > 1) {
            html += `<li class="page-item">
                <a class="page-link" href="#" onclick="LogsViewer.goToPage(${this.currentPage - 1})">
                    Anterior
                </a>
            </li>`;
        }

        // Páginas
        for (let i = 1; i <= Math.min(totalPages, 10); i++) {
            const active = i === this.currentPage ? 'active' : '';
            html += `<li class="page-item ${active}">
                <a class="page-link" href="#" onclick="LogsViewer.goToPage(${i})">${i}</a>
            </li>`;
        }

        // Botón siguiente
        if (this.currentPage < totalPages) {
            html += `<li class="page-item">
                <a class="page-link" href="#" onclick="LogsViewer.goToPage(${this.currentPage + 1})">
                    Siguiente
                </a>
            </li>`;
        }

        container.innerHTML = html;
    },

    goToPage: function (page) {
        this.currentPage = page;
        this.loadLogs();
    },

    setupEventListeners: function () {
        document.getElementById('btn-refresh')?.addEventListener('click', () => {
            this.loadLogs();
        });
    }
};