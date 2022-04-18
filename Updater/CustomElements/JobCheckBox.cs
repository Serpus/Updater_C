using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

/// <summary>
/// Класс для помещения окружения джоба дженкина в чекбокс
/// </summary>
namespace Updater
{
    internal class JobCheckBox : CheckBox
    {
        public String JobName { get; set; }
        public String JobUrl { get; set; }
        public String JobProject { get; set; }
    }
}
