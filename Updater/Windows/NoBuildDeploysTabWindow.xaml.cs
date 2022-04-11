using NLog.Fluent;
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

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для NoBuildDeploysTabWindow.xaml
    /// </summary>
    public partial class NoBuildDeploysTabWindow : Window
    {
        public NoBuildDeploysTabWindow()
        {
            InitializeComponent();

            this.Closing += NoBuildDeploysTabWindow_Closed;
        }

        private void NoBuildDeploysTabWindow_Closed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Data.IsCloseProgram)
            {
                e.Cancel = false;
            }
            Log.Info("Закрываем окно деплоев без сборки билдов");
            this.Visibility = Visibility.Hidden;
            this.Owner.Show();
        }
    }
}
