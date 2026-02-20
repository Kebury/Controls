using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Controls.Helpers
{
    /// <summary>
    /// Attached property для исправления скролла колесиком мыши в ScrollViewer
    /// </summary>
    public static class ScrollBehavior
    {
        public static readonly DependencyProperty EnableMouseWheelScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableMouseWheelScroll",
                typeof(bool),
                typeof(ScrollBehavior),
                new PropertyMetadata(false, OnEnableMouseWheelScrollChanged));

        public static bool GetEnableMouseWheelScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableMouseWheelScrollProperty);
        }

        public static void SetEnableMouseWheelScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableMouseWheelScrollProperty, value);
        }

        private static void OnEnableMouseWheelScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
                }
                else
                {
                    element.PreviewMouseWheel -= Element_PreviewMouseWheel;
                }
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is UIElement element && !e.Handled)
            {
                var scrollViewer = FindScrollViewer(element);
                if (scrollViewer != null)
                {
                    if (e.Delta > 0)
                    {
                        scrollViewer.LineUp();
                        scrollViewer.LineUp();
                        scrollViewer.LineUp();
                    }
                    else
                    {
                        scrollViewer.LineDown();
                        scrollViewer.LineDown();
                        scrollViewer.LineDown();
                    }
                    e.Handled = true;
                }
            }
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;

            DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is ScrollViewer sv)
                    return sv;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return FindScrollViewerInChildren(element);
        }

        private static ScrollViewer? FindScrollViewerInChildren(DependencyObject element)
        {
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i);
                var result = FindScrollViewerInChildren(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
