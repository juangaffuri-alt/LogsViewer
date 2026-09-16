using LogsViewer.Models;

namespace LogsViewer.Services.Contracts
{
    public interface ILogService
    {
        /// <summary>
        /// Obtiene logs de forma paginada
        /// </summary>
        Task<List<LogViewModel>> GetLogsAsync(int page = 1, int pageSize = 50);

        /// <summary>
        /// Obtiene el total de logs disponibles
        /// </summary>
        Task<int> GetLogsCountAsync();

        /// <summary>
        /// Obtiene los logs más recientes
        /// </summary>
        Task<List<LogViewModel>> GetRecentLogsAsync(int count = 10);

        /// <summary>
        /// Busca logs por query string
        /// </summary>
        Task<List<LogViewModel>> SearchLogsAsync(string query);

        /// <summary>
        /// Obtiene estadísticas de logs
        /// </summary>
        Task<LogStatisticsViewModel> GetStatisticsAsync();

        /// <summary>
        /// Obtiene datos de línea de tiempo para gráficos
        /// </summary>
        Task<TimeSeriesDataViewModel> GetTimeSeriesDataAsync(string timeRange = "24h");

        /// <summary>
        /// Obtiene los errores más frecuentes
        /// </summary>
        Task<List<TopErrorViewModel>> GetTopErrorsAsync(int limit = 10);

        /// <summary>
        /// Filtra logs por nivel
        /// </summary>
        Task<List<LogViewModel>> GetLogsByLevelAsync(string level, int limit = 100);

        /// <summary>
        /// Filtra logs por fuente/aplicación
        /// </summary>
        Task<List<LogViewModel>> GetLogsBySourceAsync(string source, int limit = 100);

        /// <summary>
        /// Búsqueda avanzada con múltiples filtros
        /// </summary>
        Task<List<LogViewModel>> AdvancedSearchAsync(AdvancedSearchCriteria criteria);
    }
}
