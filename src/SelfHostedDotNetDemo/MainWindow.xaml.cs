using System.Windows;
using System.Linq;
using System;

namespace SelfHostedJSONExample
{
    public partial class MainWindow : Window
    {
        private ServiceController _controller;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtPassword.Text = GenerateRandomPassword();
            txtPort.Text = "8080";
            txtSSID.Text = String.Format("{0}net", ServiceController.UserName);
        }

        private string GenerateRandomPassword()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_controller == null || !_controller.IsRunning)
            {
                int port;
                if (!Int32.TryParse(txtPort.Text, out port))
                {
                    MessageBox.Show("Invalid Port.");
                    return;
                }
                if (txtPassword.Text == string.Empty || txtPassword.Text.Length < 8)
                {
                    MessageBox.Show("Invalid passord. Please use an 8 character alphanumeric value.");
                    return;
                }
                if (txtSSID.Text == string.Empty || txtSSID.Text.Length < 5)
                {
                    MessageBox.Show("SSID must be a 5 character string.");
                    return;
                }

                _controller = new ServiceController(port, txtSSID.Text, txtPassword.Text);
                _controller.StatusEvent += new EventHandler<StatusEventArgs>(_controller_StatusEvent);
                _controller.Start();
                btnStartStop.Content = "Stop";
            }
            else
            {
                _controller.Stop();
                _controller.StatusEvent -= _controller_StatusEvent;
                _controller = null;
                btnStartStop.Content = "Start";
            }
        }

        void _controller_StatusEvent(object sender, StatusEventArgs e)
        {
            var msg = e.Detail;
            if (e.Severity > 0) 
            {
                msg += String.Format(" Severity ({0}).", e.Severity);
            }
            msg += Environment.NewLine;
            txtConsole.Text += msg;
        }
    }
}
