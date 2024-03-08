using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatorConfigurator.Core
{
    public class ComboBoxItem<T>
    {
        public T Item { get; set; }
        public bool IsChecked { get; set; }

        public ComboBoxItem(T item, bool initalValue)
        {
            Item = item;
            IsChecked = initalValue;
        }
    }
}
