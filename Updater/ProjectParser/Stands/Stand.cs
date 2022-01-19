using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    abstract class Stand
    {
        public String Name { get; set; }
        public String DeploymentProjectId { get; set; }
        public Key Key { get; set; }
    }
}
