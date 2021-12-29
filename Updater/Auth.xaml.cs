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
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Auth : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public Auth()
        {
            InitializeComponent();
        }

        Requests request = new Requests();

        private void Login(object sender, RoutedEventArgs e)
        {

            if (username.Text == "")
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            Data.username = username.Text;
            Data.password = password.Password;

            Log.Info("Авторизация под логином: " + username.Text);

            string result = Requests.getRequest("https://ci-sel.dks.lanit.ru/rest/api/latest/deploy/project/all");

            if (result == null)
            {
                errorText.Content = Requests.error;
                MessageBox.Show($"Что-то пошло не так: {Requests.error}. Попробуйте ещё раз");
                return;
            }

            Data.projects = JsonConvert.DeserializeObject<Project[]>(result);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
