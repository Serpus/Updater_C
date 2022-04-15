using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
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
                            id = environment.id,
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
                            id = environment.id,
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
                            id = environment.id,
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
                            id = environment.id,
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
                            id = environment.id,
                            Name = environment.name,
                            DeploymentProjectId = environment.deploymentProjectId,
                            Key = environment.key
                        };

                        project.stands.Add(eis7);
                    }
                }
            }
        }

        private void CheckUpdates(object sender, RoutedEventArgs e)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://s101238@s101238.hostru10.fornex.host/public_ftp/version");
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential("du@s101238.hostru10.fornex.host", "emtzfwsn7q8stuw0xm");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(responseStream);
            String remoteVersion = reader.ReadToEnd().Replace("version/.\r\nversion/..\r\nversion/", "").Replace("\r\n", "");

            IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
            if (double.Parse(remoteVersion, formatter) > Data.localVersion)
            {
                if (MessageBox.Show($"Обнаружена новая версия программы ({remoteVersion}). Обновить?", "Обновление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (!Directory.Exists("InstallUpdateForUpdater"))
                    {
                        MessageBox.Show("Папка InstallUpdateForUpdater для обнолвения отсутствует. Не получится обновить");
                        return;
                    }

                    Log.Info("Запускаем обновление");
                    Process.Start(Environment.CurrentDirectory + "/InstallUpdateForUpdater/InstallUpdateForUpdater.exe");
                    Application.Current.Shutdown();
                }
            } else
            {
                MessageBox.Show("Новых обновлений нет");
            }
        }
    }
}
