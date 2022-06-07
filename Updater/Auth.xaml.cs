using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Meziantou.Framework.Win32;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Auth : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        private BackgroundWorker checkUpdatesWorker = new BackgroundWorker();
        private bool loading = false;
        private string remoteVersion = "-1";

        public Auth()
        {
            InitializeComponent();

            checkUpdatesWorker.WorkerReportsProgress = true;
            checkUpdatesWorker.WorkerSupportsCancellation = true;
            checkUpdatesWorker.DoWork += CheckUpdatesWorker_DoWork; ;
            checkUpdatesWorker.ProgressChanged += CheckUpdatesWorker_ProgressChanged; ;
            checkUpdatesWorker.RunWorkerCompleted += CheckUpdatesWorker_RunWorkerCompleted; ;

            Version.Content = $"v. {Data.GetVersion()}";

            Log.Info("\n--- *** Launch Updater *** ---");
            Log.Info("Environment Version: " + Environment.Version);

            string subKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion";
            RegistryKey key = Microsoft.Win32.Registry.LocalMachine;
            RegistryKey skey = key.OpenSubKey(subKey);
            string name = skey.GetValue("ProductName").ToString();
            Log.Info("Environment OSVersion: " + Environment.OSVersion + " - " + name);

            Log.Info("Environment Is64BitOperatingSystem: " + Environment.Is64BitOperatingSystem);

            CultureInfo ci = CultureInfo.InstalledUICulture;
            Log.Info("Default Language Info:");
            Log.Info("* Name: {0}", ci.Name);
            Log.Info("* Display Name: {0}", ci.DisplayName);
            Log.Info("* English Name: {0}", ci.EnglishName);
            Log.Info("* 2-letter ISO Name: {0}", ci.TwoLetterISOLanguageName);
            Log.Info("* 3-letter ISO Name: {0}", ci.ThreeLetterISOLanguageName);
            Log.Info("* 3-letter Win32 API Name: {0}", ci.ThreeLetterWindowsLanguageName);

            getOperatingSystemInfo();
            getProcessorInfo();

            if (CredentialManager.ReadCredential("Updater - " + Environment.UserName) != null)
            {
                username.Text = CredentialManager.ReadCredential("Updater - " + Environment.UserName).UserName.ToString();
                password.Password = CredentialManager.ReadCredential("Updater - " + Environment.UserName).Password.ToString();
            }
        }

        public void getOperatingSystemInfo()
        {
            //Create an object of ManagementObjectSearcher class and pass query as parameter.
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                {
                    Log.Info("Operating System Name  :  " + managementObject["Caption"].ToString());   //Display operating system caption
                }
                if (managementObject["OSArchitecture"] != null)
                {
                    Log.Info("Operating System Architecture  :  " + managementObject["OSArchitecture"].ToString());   //Display operating system architecture.
                }
                if (managementObject["CSDVersion"] != null)
                {
                    Log.Info("Operating System Service Pack   :  " + managementObject["CSDVersion"].ToString());     //Display operating system version.
                }
            }
        }

        public void getProcessorInfo()
        {
            RegistryKey processor_name = Registry.LocalMachine.OpenSubKey(@"Hardware\Description\System\CentralProcessor\0", RegistryKeyPermissionCheck.ReadSubTree);   //This registry entry contains entry for processor info.

            if (processor_name != null)
            {
                if (processor_name.GetValue("ProcessorNameString") != null)
                {
                    Log.Info(processor_name.GetValue("ProcessorNameString"));   //Display processor ingo.
                }
            }
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
            CredentialManager.WriteCredential("Updater - " + Environment.UserName, username.Text, password.Password, CredentialPersistence.LocalMachine);


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
            try 
            {
                checkUpdatesWorker.RunWorkerAsync();
            } catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void Update()
        {
            IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
            String LocalVersionFormatted = Data.GetVersion();

            if (double.Parse(remoteVersion, formatter) > Data.localVersion)
            {
                if (MessageBox.Show($"Обнаружена новая версия программы ({remoteVersion}). Ваша версия: {LocalVersionFormatted}. Обновить ?",
                    "Обновление", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
            }
            else if (remoteVersion.Equals("-1"))
            {
                MessageBox.Show("Ошибка проверки обновлений", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            } else
            {
                MessageBox.Show("Новых обновлений нет. Ваша версия: " + LocalVersionFormatted, "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }





        private void CheckUpdatesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            checkUpdatesWorker.ReportProgress(50);
            Log.Info("UPDATE MODULE: get ftp://intemulator@192.168.231.17/Updater/v");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://intemulator@192.168.231.17/Updater/v");
            Log.Info("UPDATE MODULE: request created");
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential("intemulator", "5Whd4mqsYj");
            Log.Info("UPDATE MODULE: get response");
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Log.Info("UPDATE MODULE: response - " + response.WelcomeMessage);
            Stream responseStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(responseStream);
            remoteVersion = reader.ReadToEnd().Replace("v/", "").Trim();
            Log.Info("remote version - " + remoteVersion);
        }


        private void CheckUpdatesWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!loading)
            {
                startLoading();
            }
        }

        private void CheckUpdatesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopLoading();
            Update();
        }

        private void startLoading()
        {
            LoadingGrid.Visibility = Visibility.Visible;
            loading = true;
        }

        private void startLoading(string loadingLabel)
        {
            LoadingGrid.Visibility = Visibility.Visible;
            LoadingLabel.Content = loadingLabel;
            loading = true;
        }

        private void stopLoading()
        {
            LoadingGrid.Visibility = Visibility.Hidden;
            loading = false;
        }
    }
}
