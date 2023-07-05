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

namespace Surface_Editor
{
    /// <summary>
    /// Interaction logic for SetPositionWindow.xaml
    /// </summary>
    public partial class SetPositionWindow : Window
    {
        public enum Source
        {
            ListView,
            Selection
        }

        public bool SetXEnable { get; set; } = false;
        public bool SetYEnable { get; set; } = false;
        public bool SetZEnable { get; set; } = false;

        public float XVal { get; set; }
        public float YVal { get; set; }
        public float ZVal { get; set; }

        public Source ToChange = Source.ListView;

        public SetPositionWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!SetXEnable && !SetYEnable && !SetZEnable)
            {
                Info.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Set_Checked(object sender, RoutedEventArgs e)
        {
            Info.Visibility = Visibility.Collapsed;
        }

        private void FromList_Checked(object sender, RoutedEventArgs e)
        {
            ToChange = Source.ListView;
        }

        private void FromSelection_Checked(object sender, RoutedEventArgs e)
        {
            ToChange = Source.Selection;
        }
    }
}
