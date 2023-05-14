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
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private List<LoggingEvent> buffer = new List<LoggingEvent>();
        private Timer timer;

        public string TextBoxName { get; set; }
        private const int MaxMessages = 100; // Maximum number of messages to be shown in the log

        public TextBoxAppender()
        {
            timer = new Timer(Flush, null, 0, 2000); // Flush every 2 seconds
        }

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
                    buffer.Add(loggingEvent);
                }
            }
        }

        private void Flush(object state)
        {
            lock (buffer)
            {
                if (buffer.Count > 0)
                {
                    textBox.Dispatcher.Invoke(() =>
                    {
                        foreach (var loggingEvent in buffer)
                        {
                            textBox.AppendText(RenderLoggingEvent(loggingEvent));
                        }
                        buffer.Clear();

                        // limit number of lines
                        const int maxLines = MaxMessages;
                        if (textBox.LineCount > maxLines)
                        {
                            int endOfLine = textBox.GetCharacterIndexFromLineIndex(maxLines);
                            textBox.Text = textBox.Text.Remove(0, endOfLine);
                        }
                    });
                }
            }
        }
    }
}
