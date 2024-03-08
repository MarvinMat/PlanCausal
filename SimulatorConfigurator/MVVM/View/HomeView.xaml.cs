using System.IO;
using System.Text;
using System.Windows.Controls;

namespace SimulatorConfigurator.MVVM.View;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        System.Console.SetOut(new ConsoleOutputter(textBox));
        System.Console.WriteLine("Hello World!");
    }
    public class ConsoleOutputter : TextWriter
    {
        TextBox textBox = null;

        public ConsoleOutputter(TextBox output)
        {
            textBox = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            textBox.Dispatcher.Invoke(() =>
            {
                textBox.AppendText(value.ToString()); // When character data is written, append it to the TextBox text
            });
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
