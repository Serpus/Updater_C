using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    public class Jobs
    {
        public Job[] jobs { get; set; }

        public override string ToString()
        {
            string str = "";
            foreach (Job job in jobs)
            {
                str += job.name + "; ";
            }
            return str;
        }
    }
}
