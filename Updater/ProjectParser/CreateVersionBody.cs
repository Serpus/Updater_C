using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class CreateVersionBody
    {
        public string planResultKey { get; set; }
        public string name { get; set; }
        public string nextVersionName { get; set; }
    }
}
