/**
 * Charts Module
 * Renders the ApexCharts visualizations fed by data the server embeds
 * in window.timelineData / window.levelDistribution (see Home/Dashboard views).
 */
var Charts = (function () {
    'use strict';

    var LEVEL_COLORS = {
        error: '#d82c0d',
        warning: '#f5a700',
        info: '#0078d4',
        debug: '#8a8f98',
        trace: '#c2c6cc'
    };

    function renderTimeline(elementId, data) {
        var el = document.getElementById(elementId);
        if (!el || typeof ApexCharts === 'undefined' || !data || !data.timestamps || data.timestamps.length === 0) {
            if (el) {
                el.innerHTML = '<div class="chart-empty">No hay datos suficientes en este rango para graficar.</div>';
            }
            return;
        }

        var options = {
            chart: {
                type: 'area',
                height: '100%',
                toolbar: { show: false },
                zoom: { enabled: false },
                fontFamily: 'inherit'
            },
            series: [
                { name: 'Error', data: data.error },
                { name: 'Warning', data: data.warning },
                { name: 'Info', data: data.info },
                { name: 'Debug', data: data.debug }
            ],
            colors: [LEVEL_COLORS.error, LEVEL_COLORS.warning, LEVEL_COLORS.info, LEVEL_COLORS.debug],
            xaxis: {
                categories: data.timestamps,
                labels: { rotate: -45, style: { fontSize: '11px' } }
            },
            yaxis: { labels: { formatter: function (v) { return Math.round(v); } } },
            dataLabels: { enabled: false },
            stroke: { curve: 'smooth', width: 2 },
            fill: { type: 'gradient', gradient: { opacityFrom: 0.4, opacityTo: 0.05 } },
            legend: { position: 'top' },
            tooltip: { shared: true, intersect: false }
        };

        var chart = new ApexCharts(el, options);
        chart.render();
    }

    function renderLevelDonut(elementId, dist) {
        var el = document.getElementById(elementId);
        if (!el || typeof ApexCharts === 'undefined' || !dist) {
            return;
        }

        var labels = [];
        var series = [];
        var colors = [];

        [['Error', dist.error, LEVEL_COLORS.error],
        ['Warning', dist.warning, LEVEL_COLORS.warning],
        ['Info', dist.info, LEVEL_COLORS.info],
        ['Debug', dist.debug, LEVEL_COLORS.debug],
        ['Trace', dist.trace, LEVEL_COLORS.trace]].forEach(function (item) {
            if (item[1] > 0) {
                labels.push(item[0]);
                series.push(item[1]);
                colors.push(item[2]);
            }
        });

        if (series.length === 0) {
            el.innerHTML = '<div class="chart-empty">Todavía no hay logs para graficar.</div>';
            return;
        }

        var chart = new ApexCharts(el, {
            chart: { type: 'donut', height: '100%', fontFamily: 'inherit' },
            series: series,
            labels: labels,
            colors: colors,
            legend: { position: 'bottom' },
            dataLabels: { enabled: true, formatter: function (val) { return val.toFixed(1) + '%'; } }
        });
        chart.render();
    }

    function init() {
        if (window.timelineData) {
            renderTimeline('timeline-chart', window.timelineData);
        }
        if (window.levelDistribution) {
            renderLevelDonut('level-distribution-chart', window.levelDistribution);
        }
    }

    return {
        init: init,
        renderTimeline: renderTimeline,
        renderLevelDonut: renderLevelDonut
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', Charts.init);
} else {
    Charts.init();
}