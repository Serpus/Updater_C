using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class BranchList
    {
        public Branches branches { get; set; }

        public override string ToString()
        {
            return "branches: { " + branches.ToString() + " }" +
                "\n";
        }
    }
}
