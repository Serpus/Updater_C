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
using Updater.MO;

namespace Updater
{
    /// <summary>
    /// Логика взаимодействия для NoBuildDeploysTabWindow.xaml
    /// </summary>
    public partial class NoBuildDeploysWindow : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        public NoBuildDeploysWindow()
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
            Log.Info("Закрываем окно деплоев без предварительной сборки");
            this.Visibility = Visibility.Hidden;
            this.Owner.Show();
        }

        private void Stand1Plus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Visible;
            Stand2Panel.Visibility = Visibility.Visible;
            Stand1Panel.Visibility = Visibility.Hidden;
        }

        private void Stand2Plus(object sender, RoutedEventArgs e)
        {
            Stand3.Visibility = Visibility.Visible;
            Stand3Panel.Visibility = Visibility.Visible;
            Stand2Panel.Visibility = Visibility.Hidden;
        }

        private void Stand2Minus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Hidden;
            Stand2Panel.Visibility = Visibility.Hidden;
            Stand1Panel.Visibility = Visibility.Visible;
            Stand2.SelectedIndex = -1;
        }

        private void Stand3Minus(object sender, RoutedEventArgs e)
        {
            Stand2.Visibility = Visibility.Visible;
            Stand2Panel.Visibility = Visibility.Visible;
            Stand3Panel.Visibility = Visibility.Hidden;
            Stand3.Visibility = Visibility.Hidden;
            Stand3.SelectedIndex = -1;
        }

        private void selectStands_Click(object sender, RoutedEventArgs e)
        {
            List<Stand> stands = new List<Stand>();
            foreach (var children in selectStandsGrid.Children)
            {
                if (children is ComboBox)
                {
                    ComboBox cb = children as ComboBox;
                    if (cb.Text != null)
                    {
                        switch (cb.Text)
                        {
                            case "ЕИС-3":
                                stands.Add(new EIS3() { Name = "ЕИС-3" });
                                break;
                            case "ЕИС-4":
                                stands.Add(new EIS4() { Name = "ЕИС-4" });
                                break;
                            case "ЕИС-5":
                                stands.Add(new EIS5() { Name = "ЕИС-5" });
                                break;
                            case "ЕИС-6":
                                stands.Add(new EIS6() { Name = "ЕИС-6" });
                                break;
                            case "ЕИС-7":
                                stands.Add(new EIS7() { Name = "ЕИС-7" });
                                break;
                        }
                    }
                }
            }
            if (stands.Count == 0)
            {
                Log.Info("Не выбрано ни одного стенда");
                MessageBox.Show("Выберите хотя бы один стенд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (BranchName.Text == "")
            {
                MessageBox.Show("Введите название ветки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Data.selectedStands = stands;
            Data.branchName = BranchName.Text;

            String standsList = "";
            foreach (Stand stand in stands)
            {
                standsList += stand.Name + ", ";
            }
            standsList = standsList.Substring(0, standsList.Length - 2);
            Log.Info("Выбранные стенды: " + standsList);

            selectStandsGrid.IsEnabled = false;
            selectStands.IsEnabled = false;
            openPreparedeployButton.IsEnabled = true;
            ResetStands.IsEnabled = true;
            branchSelectPanel.IsEnabled = false;
        }

        private void ResetStands_Click(object sender, RoutedEventArgs e)
        {
            ResetStands.IsEnabled = false;
            openPreparedeployButton.IsEnabled = false;
            selectStands.IsEnabled = true;
            selectStandsGrid.IsEnabled = true;
            branchSelectPanel.IsEnabled = true;
            Data.selectedStands = null;
        }

        private void SetBranchName(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
            {
                MenuItem item = (MenuItem)sender;
                BranchName.Text = item.Header.ToString();
            }
        }

        private void openPrepareDeployButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareNoBuildsDeployWindow window = new PrepareNoBuildsDeployWindow()
            {
                Owner = this,
            };
            window.ShowDialog();
        }
    }
}
