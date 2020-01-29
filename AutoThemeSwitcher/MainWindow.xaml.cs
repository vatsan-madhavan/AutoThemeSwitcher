using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.AddHook(WndProc);
            }

            // Fixup theme at start once
            UpdateApplicationTheme(this);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_THEMECHANGED:
                case User32.WindowMessage.WM_DWMCOMPOSITIONCHANGED:
                    UpdateApplicationTheme(this);
                    break;
            }

            return IntPtr.Zero;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <remarks>
        /// Updates theme recursively throughout the Visual Tree
        ///     Assumes the themes are named "Normal{TypeName}" or "HighContrast{TypeName}" respectively
        ///     
        ///     Types are matched from derived to base class onwards. For e.g., "NormalMainWindow" is matched 
        ///     before "NormalWindow" etc. 
        /// </remarks>
        private void UpdateApplicationTheme(DependencyObject d)
        {
            var types = new List<Type>();
            var type = d.GetType();

            while (!TerminalTypes.Contains(type))
            {
                types.Add(type);
                type = type.BaseType;
            }

            var baseStyleName = (!SystemParameters.HighContrast ? "Normal" : "HighContrast");

            // Need to make sure that this really does iterate over types from 
            // most-derived to least-derived. As long as List<T> and foreach are
            // order-preserving. 
            foreach (var t in types)
            {
                var styleName = baseStyleName + t.Name;

                if (TryFindResource(styleName) is Style style)
                {
                    if (d is FrameworkElement fe)
                    {
                        fe.Style = style;
                        Trace.WriteLine($"Found: {styleName}");
                        break;
                    }

                    if (d is FrameworkContentElement fce)
                    {
                        fce.Style = style;
                        Trace.WriteLine($"Found: {styleName}");
                        break;
                    }
                }
            }

            var n = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < n; i++)
            {
                UpdateApplicationTheme(VisualTreeHelper.GetChild(d, i));
            }
            
        }

        // These are the types that FrameworkElement and FrameworkContentElement
        // derive from (the walk stops at DependencyObject). 
        // FE and FFE are special since they have ".Style" property that can
        // be updated. 
        IReadOnlyList<Type> TerminalTypes => new List<Type>
            {
                typeof(Visual),
                typeof(UIElement),
                typeof(ContentElement),
                typeof(DependencyObject)
            }.AsReadOnly();
    }
}
