using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogsViewer.Infrastructure.App;

public interface IFeatureManagement
{
    bool IsFeatureEnabled(string name);
}
