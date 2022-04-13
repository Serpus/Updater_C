using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class DataJenkins
    {
        public static string username { get; set; }
        public static string password { get; set; }
        public static string ProjectName { get; set; }
        public static List<Register> Registers { get; set; }
        public static string SKIP_DB { get; set; }
        public static string STAND { get; set; }
        public static List<DeployEnvironment> DeployEnvironments { get; set;}
        public static List<BuildResult> BuildResults { get; set; }
    }
}
