using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogsViewer.Services.Contracts.Statistics;

public interface IFastStatisticsServices
{
    Task<BaseStatistics> GetBaseStatistics(CancellationToken cancellationToken);

    Task<IReadOnlyList<LogShare>> GetLevelsDistribution(CancellationToken cancellationToken);
    
    Task<IReadOnlyList<ApplicationShare>> GetApplicationsDistribution(CancellationToken cancellationToken);
}
