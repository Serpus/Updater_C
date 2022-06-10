using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Updater.ProjectParser.Jenkins.BuildResult;

namespace Updater
{
    internal class BuildResult
    {
        public ActionBuild[] actions { get; set; }
        public string FullDisplayName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Result { get; set; }

        public string getStand()
        {
            foreach (ActionBuild action in actions)
            {
                if (action.getStandParameter() != null)
                {
                    return action.getStandParameter().Value;
                }
            }
            return null;
        }
    }
}
