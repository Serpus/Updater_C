using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.ProjectParser.BuildParser
{
    internal class BuildResultsInBranch
    {
        public Results Results { get; set; }
    }

    class Results
    {
        public BuildStatus[] Result { get; set; }
    }
}
