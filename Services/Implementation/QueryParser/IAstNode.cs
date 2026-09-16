using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogsViewer.Services.Implementation.QueryParser;

public interface IAstNode
{
    void ToRql(RqlQueryBuilderContext context);
}
