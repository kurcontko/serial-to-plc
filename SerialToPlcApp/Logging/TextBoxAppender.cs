using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Concurrent;

namespace SerialToPlcApp.Logging
{
    public class TextBoxAppender : AppenderSkeleton
    {
        private TextBox textBox;
        private Queue<string> buffer = new Queue<string>();

        public string TextBoxName { get; set; }
        private const int MaxBufferSize = 100; // Maximum number of messages to be shown in the log

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (string.IsNullOrEmpty(TextBoxName))
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (textBox == null)
                {
                    MainWindow form = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;

                    if (form != null)
                        textBox = form.FindName(TextBoxName) as TextBox;
                }
            });

            if (textBox != null)
            {
                lock (buffer)
                {
                    buffer.Enqueue(RenderLoggingEvent(loggingEvent));

                    if (buffer.Count > 0)
                    {
                        textBox.Dispatcher.Invoke(() =>
                        {

                            // Remove oldest log messages from the buffer if it exceeds the max size
                            while (buffer.Count > MaxBufferSize)
                            {
                                buffer.Dequeue();
                            }

                            // Display the buffer in the TextBox
                            textBox.Text = string.Join("", buffer);
                        });
                    }
                }
            }
        }

    }
}
