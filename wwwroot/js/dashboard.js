/**
 * Dashboard Module
 * Handles dashboard rendering and interactions
 */

var Dashboard = (function() {
    'use strict';

    // Dashboard state
    var state = {
        initialized: false,
        data: {},
        widgets: []
    };

    // Initialize dashboard
    function init() {
        console.log('Dashboard initialized');
        state.initialized = true;
        renderDashboard();
    }

    // Initialize discover page
    function initDiscoverPage() {
        console.log('Initializing discover page...');
        var container = document.getElementById('discover-container');
        if (container) {
            container.innerHTML = '<div class="discover-content"><p>Discover page loaded successfully</p></div>';
        }
    }

    // Render dashboard
    function renderDashboard() {
        console.log('Rendering dashboard...');
        var container = document.getElementById('dashboard-container');
        if (container) {
            container.innerHTML = '<div class="dashboard-content"><p>Dashboard loaded successfully</p></div>';
        }
    }

    // Update dashboard data
    function updateData(newData) {
        state.data = newData;
        renderDashboard();
    }

    // Add widget
    function addWidget(widget) {
        state.widgets.push(widget);
    }

    // Get dashboard state
    function getState() {
        return state;
    }

    // Load logs
    function loadLogs(page, pageSize) {
        console.log('Loading logs - Page: ' + page + ', Size: ' + pageSize);
        fetch('/api/logs?page=' + page + '&pageSize=' + pageSize)
            .then(function(response) { return response.json(); })
            .catch(function(error) { console.error('Error loading logs:', error); });
    }

    // Load statistics
    function loadStatistics() {
        console.log('Loading statistics...');
        fetch('/api/statistics')
            .then(function(response) { return response.json(); })
            .catch(function(error) { console.error('Error loading statistics:', error); });
    }

    // Public API
    return {
        init: init,
        initDiscoverPage: initDiscoverPage,
        updateData: updateData,
        addWidget: addWidget,
        getState: getState,
        loadLogs: loadLogs,
        loadStatistics: loadStatistics
    };
})();

// Initialize dashboard when ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', Dashboard.init);
} else {
    Dashboard.init();
}
