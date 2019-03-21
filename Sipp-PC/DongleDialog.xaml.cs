using System;
using System.ComponentModel;
using System.Windows;

namespace Sipp_PC
{
    /// <summary>
    /// Window1.xaml 的互動邏輯
    /// </summary>
    public partial class DongleDialog : Window
    {
        bool dongle = false;
        public DongleDialog()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Console.WriteLine("On closing");
            e.Cancel = !dongle;
            base.OnClosing(e);
        }

        public void HasDongle()
        {
            dongle = true;
        }
    }
}
