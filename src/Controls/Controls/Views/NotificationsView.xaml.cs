using System.Windows.Controls;
using System.Windows.Input;

namespace Controls.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик клика для снятия фокуса с полей ввода при клике на пустую область
        /// </summary>
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as System.Windows.DependencyObject;
            
            while (source != null && source != this)
            {
                if (source is TextBox || 
                    source is ComboBox || 
                    source is ComboBoxItem || 
                    source is Button ||
                    source is System.Windows.Controls.Primitives.Popup)
                {
                    return;
                }
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                MainGrid.Focus();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }
}
