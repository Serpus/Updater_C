using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Updater.CustomElements
{
    internal class DeployStatusLabel : Label
    {
        public StartingDeployResult DeployResult { get; set; }
        public Project Project { get; set; }

        public DeployStatusLabel(StartedDeploy deploy)
        {
            DeployResult = deploy.DeployResult;
            Project = deploy.Project;
        }
    }
}
