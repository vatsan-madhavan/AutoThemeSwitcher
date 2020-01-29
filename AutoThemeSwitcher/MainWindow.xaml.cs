using Microsoft.WPF.Samples;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfApp7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ThemeSwitcher = new ThemeSwitcher(this);
        }

        public ThemeSwitcher ThemeSwitcher { get; }

        private void SwitchToHighContrast(object sender, RoutedEventArgs e)
        {
            ThemeSwitcher.SimulateThemeChange(true);
        }

        private void SwitchToNormalTheme(object sender, RoutedEventArgs e)
        {
            ThemeSwitcher.SimulateThemeChange(false);
        }
    }
}
