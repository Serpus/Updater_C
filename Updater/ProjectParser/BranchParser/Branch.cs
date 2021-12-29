using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    internal class Branch
    {
        public string shortName { get; set; }
        public string shortKey { get; set; }
        public string key { get; set; }
        public string name { get; set; }

        public override string ToString()
        {
            return $"shortName: {shortName}\n" +
                $"shortKey: {shortKey}\n" +
                $"key: {key}\n" +
                $"name: {name}\n";
        }
    }
}
