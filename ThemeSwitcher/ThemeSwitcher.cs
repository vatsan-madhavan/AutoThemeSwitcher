using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Microsoft.WPF.Samples
{
    public class ThemeSwitcher
    {
        public ThemeSwitcher(FrameworkElement fe)
        {
            if (fe == null)
            {
                throw new ArgumentNullException(nameof(fe));
            }

            FrameworkElement = fe;

            WeakEventManager<FrameworkElement, RoutedEventArgs>.AddHandler(
                FrameworkElement, 
                nameof(System.Windows.FrameworkElement.Loaded), 
                OnLoaded);
        }

        public FrameworkElement FrameworkElement { get; }

        public void SimulateThemeChange(bool isHighContrast)
        {
            try
            {
                SimulatedHighContrast = isHighContrast;
                UpdateApplicationTheme();
            }
            finally
            {
                SimulatedHighContrast = false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (FrameworkElement is Visual v &&
                PresentationSource.FromVisual(v) is HwndSource hwndSource)
            {
                hwndSource.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((User32.WindowMessage)msg)
            {
                case User32.WindowMessage.WM_THEMECHANGED:
                    UpdateApplicationTheme();
                    break;
            }

            return IntPtr.Zero;
        }

        private void UpdateApplicationTheme()
        {
            UpdateApplicationTheme(FrameworkElement);
        }

        /// <summary>
        /// Updates theme recursively throughout the Visual Tree
        ///     Assumes that themes are named "Normal{TypeName}" or "HighContrast{TypeName}"
        ///     
        ///     If a style matching the exact type is not found, then a style for a base class is matched (if found). For e.g., 
        ///     if "NormalMainWindow" ("Normal" + TypeName="MainWindow") is not found, then "NormalWindow" ("Normal" + TypeName="Window") is 
        ///     matched. 
        /// </summary>
        /// <param name="d"></param>
        private void UpdateApplicationTheme(DependencyObject d)
        {
            Type targetType = d.GetType();
            Style newStyle = IsHighContrast ? GetHighContrastStyle(targetType) : GetNormalStyle(targetType);
            if (!DefaultStylesCache.ContainsKey(targetType))
            {
                if (d is FrameworkElement fe)
                {
                    DefaultStylesCache[targetType] = fe.Style;
                }
                else if (d is FrameworkContentElement fce)
                {
                    DefaultStylesCache[targetType] = fce.Style;
                } 
                else
                {
                    DefaultStylesCache[targetType] = null;
                }
            }

            var types = new List<Type>();

            // Enumerate the Type hierarchy from d.GetType() to DependencyObject
            var type = d.GetType();
            while (!FeFfeParentTypesToDo.Contains(type))
            {
                types.Add(type);
                type = type.BaseType;
            }


            foreach (var t in types) // List<T> is ordered, so the iteration will happen from best matching type onwards.
            {
                if (d is FrameworkElement fe)
                {
                    fe.Style = newStyle ?? DefaultStylesCache[d.GetType()];
                    break;
                }


                if (d is FrameworkContentElement fce)
                {
                    fce.Style = newStyle ?? DefaultStylesCache[t];
                    break;
                }
            }

            // Update all children recursively
            var nChildren = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < nChildren; i++)
            {
                UpdateApplicationTheme(VisualTreeHelper.GetChild(d, i));
            }
        }

        private Style GetNormalStyle(Type targetType)
        {
            return GetStyleByThemeSwitcherNamingConvention(
                FrameworkElement,
                targetType,
                NormalStylesCache,
                "Normal");
        }

        private Style GetHighContrastStyle(Type targetType)
        {
            return GetStyleByThemeSwitcherNamingConvention(
                FrameworkElement,
                targetType,
                HighContrastStylesCache,
                "HighContrast");
        }

        private static Style GetStyleByThemeSwitcherNamingConvention(
            FrameworkElement resourceRoot, 
            Type targetType, 
            Dictionary<Type, Style> styleCache, 
            string styleNamePrefix)
        {
            if (!styleCache.TryGetValue(targetType, out Style cachedStyle))
            {
                var styleName = $"{styleNamePrefix}{targetType.Name}";
                if (resourceRoot.TryFindResource(styleName) is Style style)
                {
                    styleCache[targetType] = style;
                }
                else
                {
                    styleCache[targetType] = null;
                }
            }

            Debug.Assert(styleCache.ContainsKey(targetType));
            return styleCache[targetType];
        }

        /// <summary>
        /// Types that <see cref="FrameworkElement"/> and <see cref="FrameworkContentElement"/>
        /// derive from (the walk stops at <see cref="DependencyObject"/>). 
        /// 
        /// <see cref="FrameworkElement"/> and <see cref="FrameworkContentElement"/> are special
        /// since they each have <see cref="FrameworkElement.StyleProperty"/> and 
        /// <see cref="FrameworkContentElement.StyleProperty"/> respectively. 
        /// </summary>
        private static IReadOnlyList<Type> FeFfeParentTypesToDo => new List<Type>
        {
            typeof(Visual),
            typeof(UIElement),
            typeof(ContentElement),
            typeof(DependencyObject)
        }.AsReadOnly();

        private bool IsHighContrast => 
            SystemParameters.HighContrast || 
            SimulatedHighContrast;

        private bool SimulatedHighContrast { get; set; } = false;

        /// <summary>
        /// A cache of default styles, if any, that is overwritten by the theme switcher with a new style. 
        /// </summary>
        [ThreadStatic]
        private static Dictionary<Type, Style> DefaultStylesCache = new Dictionary<Type, Style>();

        /// <summary>
        /// A cache of "Normal{TypeName}" styles
        /// </summary>
        [ThreadStatic]
        private static Dictionary<Type, Style> NormalStylesCache = new Dictionary<Type, Style>();

        /// <summary>
        /// A cache of "HighContrast{TypeName}" styles
        /// </summary>
        [ThreadStatic]
        private static Dictionary<Type, Style> HighContrastStylesCache = new Dictionary<Type, Style>();
    }
}
