using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.CustomElements
{
    internal class CompareProjects
    {
        public static int CompareBranch(Project x, Project y)
        {
            Console.WriteLine("x - " + x.branch.name + ", y - " + y.branch.name);
            if (x.branch.name.Contains("PRIV"))
            {
                if (y.branch.name.Contains("PRIV"))
                {
                    return 0;
                } else
                {
                    return 1;
                }
            }
            if (x.branch.name.Contains("LKP"))
            {
                if (y.branch.name.Contains("PRIV"))
                {
                    return -1;
                }
                if (y.branch.name.Contains("LKP"))
                {
                    return 0;
                } else 
                {
                    return 1; 
                }
            }
            if (y.branch.name.Contains("PRIV"))
            {
                if (x.branch.name.Contains("PRIV"))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            if (y.branch.name.Contains("LKP"))
            {
                if (x.branch.name.Contains("PRIV"))
                {
                    return 1;
                }
                if (x.branch.name.Contains("LKP"))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            } else
            {
                return 0;
            }
        }
    }
}
