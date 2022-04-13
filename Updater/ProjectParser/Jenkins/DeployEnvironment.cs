using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class DeployEnvironment
    {
        public string RegisterName { get; set; }
        public string Project { get; set; }
        public string Branch { get; set; }
        public string Stand { get; set; }
        public string SKIP_DB { get; set; }
    }
}
