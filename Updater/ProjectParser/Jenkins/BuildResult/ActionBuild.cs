using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.ProjectParser.Jenkins.BuildResult
{
    internal class ActionBuild
    {
        public Cause[] Causes { get; set; }
        public Parameter[] Parameters { get; set; }

        public Parameter getStandParameter()
        {
            if (Parameters == null)
            {
                return null;
            }
            foreach (Parameter parameter in Parameters)
            {
                if (parameter.Name == "STAND")
                {
                    return parameter;
                }
            }
            return null;
        }
    }
}
