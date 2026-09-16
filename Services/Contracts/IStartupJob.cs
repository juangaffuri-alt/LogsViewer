using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogsViewer.Services.Contracts;

public interface IStartupJob
{
    ValueTask Execute(CancellationToken cancellationToken);
}
