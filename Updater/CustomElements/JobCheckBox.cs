using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Updater
{
    internal class JobCheckBox : CheckBox
    {
        public String JobName { get; set; }
        public String JobUrl { get; set; }
    }
}
