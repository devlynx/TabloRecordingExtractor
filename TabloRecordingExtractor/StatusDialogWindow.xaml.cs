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

namespace TabloRecordingExtractor
{
    /// <summary>
    /// Interaction logic for Status.xaml
    /// </summary>
    public partial class StatusDialogWindow : Window
    {
        public StatusDialogWindow()
        {
            InitializeComponent();
        }

        public void AddStatusText(string StatusText)
        {
            tbStatus.Text += StatusText + "\n";
            svStatus.ScrollToBottom();
        }
    }
}
