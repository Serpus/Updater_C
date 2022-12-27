using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
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
    /// Логика взаимодействия для FailedBuildsMo.xaml
    /// </summary>
    public partial class FailedBuildsMo : Window
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        ItemCollection ItemCollection { get; set; }
        public FailedBuildsMo(ItemCollection itemCollection)
        {
            ItemCollection = itemCollection;
            InitializeComponent();
            GenerateList();
        }

        private void GenerateList()
        {
            foreach (TabItem item in ItemCollection)
            {
                if (item.Content is ListBox)
                {
                    ListBox listBox = (ListBox)item.Content;
                    FailedBuildsList.Text += item.Header + ":\n";
                    foreach (BuildStatusLabel label in listBox.Items)
                    {
                        if (label.BuildResult.Result.Equals("FAILURE"))
                        {
                            Hyperlink link = new Hyperlink(new Run())
                            {
                                NavigateUri = new Uri(label.BuildResult.Url),
                            };
                            FailedBuildsList.Text += label.BuildResult.Url.ToString() + "\n";
                            Log.Info($"Add failed build {label.BuildResult.FullDisplayName} to MO");
                        }
                    }
                }
                else
                {
                    Log.Info($"{item.Content} not in ListBox");
                }
            }
        }

        private void GenerateListWithName()
        {
            foreach (TabItem item in ItemCollection)
            {
                if (item.Content is ListBox)
                {
                    ListBox listBox = (ListBox)item.Content;
                    FailedBuildsList.Text += item.Header + ":\n";
                    foreach (BuildStatusLabel label in listBox.Items)
                    {
                        Hyperlink link = new Hyperlink(new Run())
                        {
                            NavigateUri = new Uri(label.BuildResult.Url),
                        };
                        FailedBuildsList.Text += label.BuildResult.FullDisplayName + "\n";
                        FailedBuildsList.Text += label.BuildResult.Url.ToString() + "\n";
                    }
                }
                else
                {
                    Log.Info($"{item.Content} not in ListBox");
                }
            }
        }

        private void OnlyLinks_Click(object sender, RoutedEventArgs e)
        {
            FailedBuildsList.Clear();
            GenerateList();
        }

        private void WithName_Click(object sender, RoutedEventArgs e)
        {
            FailedBuildsList.Clear();
            GenerateListWithName();
        }

        private void CopyToBuffer_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(FailedBuildsList.Text);
        }
    }
}
