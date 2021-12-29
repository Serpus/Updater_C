using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class Branches
    {
        public string size { get; set; }
        public Branch[] branch { get; set; }

        public override string ToString()
        {
            return $"size: {size}\n" +
                "branch: [" + branch + "]\n";
        }
    }
}
