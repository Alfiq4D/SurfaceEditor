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
    /// Interaction logic for SelectModelWindow.xaml
    /// </summary>
    public partial class SelectModelWindow : Window
    {
        public enum SelectedModelType
        {
            Torus,
            AstroidalEllipsoid,
            BohemianDome,
            Coil,
            Cornucopia,
            Cross_Cap,
            Elasticity,
            Ellipsoid,
            Figure8,
            Horn,
            KlainBottle,
            MorinsSurface,
            Pear,
            Sea_Shell,
            SineTorus,
            Sphere
        }

        public SelectedModelType Type { get; set; }

        public SelectModelWindow()
        {
            DataContext = this;
            InitializeComponent();
            Type = SelectedModelType.Torus;
        }

        private void Torus_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Torus;
        }

        private void AstroidalEllipsoid_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.AstroidalEllipsoid;
        }

        private void BohemianDome_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.BohemianDome;
        }

        private void Coil_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Coil;
        }

        private void Cornucopia_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Cornucopia;
        }

        private void Cross_Cap_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Cross_Cap;
        }

        private void Elasticity_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Elasticity;
        }

        private void Ellipsoid_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Ellipsoid;
        }

        private void Figure8_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Figure8;
        }

        private void Horn_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Horn;
        }

        private void KlainBottle_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.KlainBottle;
        }

        private void MorinsSurface_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.MorinsSurface;
        }

        private void Pear_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Pear;
        }

        private void Sea_Shell_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Sea_Shell;
        }

        private void SineTorus_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.SineTorus;
        }

        private void Sphere_Checked(object sender, RoutedEventArgs e)
        {
            Type = SelectedModelType.Sphere;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
