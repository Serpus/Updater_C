using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Updater.CustomElements
{
    public class ChangeLogVersion : TextBlock
    {
        public ChangeLogVersion(string version)
        {
            Text = version;
        }
    }
}
