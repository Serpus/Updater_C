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
    /// Логика взаимодействия для TestMo.xaml
    /// </summary>
    public partial class TestMo : Window
    {
        public TestMo()
        {
            InitializeComponent();
        }


        public void TestSetBuildResultInUi(object sender, RoutedEventArgs e)
        {
            HashSet<string> StandSet = new HashSet<string>();
            List<BuildStatusLabel> labelList = new List<BuildStatusLabel>();

            labelList = TestData.GetBuildStatusLabels(4, TestData.FailedResultBuild);
            labelList.AddRange(TestData.GetBuildStatusLabels(4, TestData.SuccessResultBuild));

            ListBox listBox = new ListBox();
            foreach (BuildStatusLabel label in labelList)
            {
                listBox.Items.Add(label);
            }
            App.jenkinsWindow.BuildStatusTabs.Items.Add(new TabItem { Header = $"ЕИС-TEST {++TestData.TestStandCounter}", Content = listBox, IsSelected = true });
        }
        public void TestClearBuildResultInUi(object sender, RoutedEventArgs e)
        {
            App.jenkinsWindow.BuildStatusTabs.Items.Clear();
        }

        public void TestEnableDisableBuildStatusGrid(object sender, RoutedEventArgs e)
        {
            bool b = App.jenkinsWindow.BuildStatusGrid.IsEnabled;
            if (b)
            {
                App.jenkinsWindow.BuildStatusGrid.IsEnabled = false; 
            } else
            {
                App.jenkinsWindow.BuildStatusGrid.IsEnabled = true;
            }
        }
    }
}
