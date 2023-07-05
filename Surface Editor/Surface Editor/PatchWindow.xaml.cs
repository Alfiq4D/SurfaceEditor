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
    /// Interaction logic for PatchWindow.xaml
    /// </summary>
    public partial class PatchWindow : Window
    {
        public enum PatchType
        {
            Rectangle,
            Cyllinder
        }

        public PatchType Type { get; set; }

        public int U { get; set; } = 2;
        public int V { get; set; } = 2;
        public int RectangleWidth { get; set; } = 20;
        public int RectangleHeight { get; set; } = 20;
        public int CylinderHeight { get; set; } = 20;
        public int CylinderRadius { get; set; } = 2;

        public PatchWindow()
        {
            DataContext = this;
            InitializeComponent();
            Type = PatchType.Rectangle;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cylinder_Checked(object sender, RoutedEventArgs e)
        {
            Type = PatchType.Cyllinder;
        }

        private void Rectangle_Checked(object sender, RoutedEventArgs e)
        {
            Type = PatchType.Rectangle;
        }
    }
}
