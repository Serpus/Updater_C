using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Updater.CustomElements;

namespace Updater.MO
{
    /// <summary>
    /// Логика взаимодействия для ChangeLogWindow.xaml
    /// </summary>
    public partial class ChangeLogWindow : Window
    {
        public ChangeLogWindow()
        {
            InitializeComponent();

            SetChangesInMo();
        }

        private void SetChangesInMo()
        {
            Dictionary<string, List<string>> dictionary = GetChanges();
            foreach (string key in dictionary.Keys)
            {
                ChangesList_SPanel.Children.Add(new Label());
                ChangesList_SPanel.Children.Add(new ChangeLogVersion(key));
                foreach (string value in dictionary[key])
                {
                    ChangesList_SPanel.Children.Add(new ChangesTextBox(value));
                }
            }
        }

        private Dictionary<string, List<string>> GetChanges()
        {
            Dictionary<string, List<string>> _changes = new Dictionary<string, List<string>>();

            _changes.Add("2.14", new List<string> { "Удаление db-adm(Golden Gate)", "Добавление ченджлога" });
            _changes.Add("2.13", new List<string> { "Добавление папки COMMON в сборку Дженкинса" });
            _changes.Add("2.11 - 2.12", new List<string> { "Добавление уведомлений для сборок в бамбу", "Исправление ошибок" });
            _changes.Add("2.10", new List<string> { "Исправлен отбор билдов для бамбу" });

            return _changes;
        }
        
    }
}
