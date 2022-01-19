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

            SetEnvironmetsStands();

            MainWindow mainWindow = new MainWindow();
            this.Close();
            mainWindow.Show();
        }

        private void SetEnvironmetsStands()
        {
            foreach (Project project in Data.projects)
            {
                project.stands = new List<Stand>();

                foreach (EnvironmentBuild environment in project.environments)
                {
                    String name = environment.name;
                    if (name.Contains("ЕИС-3"))
                    {
                        EIS3 eis3 = new EIS3
                        {
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis3);
                    }

                    if (name.Contains("ЕИС-4"))
                    {
                        EIS4 eis4 = new EIS4
                        {
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis4);
                    }

                    if (name.Contains("ЕИС-5"))
                    {
                        EIS5 eis5 = new EIS5
                        {
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis5);
                    }

                    if (name.Contains("ЕИС-6"))
                    {
                        EIS6 eis6 = new EIS6()
                        {
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis6);
                    }

                    if (name.Contains("ЕИС-7"))
                    {
                        EIS7 eis7 = new EIS7
                        {
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis7);
                    }
                }
            }
        }
    }
}
