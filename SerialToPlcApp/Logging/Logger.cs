using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SerialToPlcApp.Logging
{
    public interface ILogger
    {
        void Log(string message);
    }
    public class Logger : ILogger
    {
        private readonly TextBox logTextBox;
        private readonly Dispatcher dispatcher;

        public Logger(TextBox logTextBox, Dispatcher dispatcher)
        {
            this.logTextBox = logTextBox;
            this.dispatcher = dispatcher;
        }

        public void Log(string message)
        {
            dispatcher.Invoke(() =>
            {
                logTextBox.AppendText($"{DateTime.Now}: {message}{Environment.NewLine}");
                logTextBox.ScrollToEnd();
            });
        }
    }
}
