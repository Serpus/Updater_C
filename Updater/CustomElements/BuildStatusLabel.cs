using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

/// <summary>
/// Класс для помещения окружения билда в Label
/// </summary>
namespace Updater.CustomElements
{
    internal class BuildStatusLabel : Label
    {
        public BuildResult BuildResult;
    }
}
