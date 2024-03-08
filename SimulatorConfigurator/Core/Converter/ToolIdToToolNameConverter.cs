using Core.Abstraction.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SimulatorConfigurator.Core.Converter
{
    public class ToolIdToToolNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int intValue && values[1] is ICollection<Tool> mappingList)
            {
                if (intValue - 1 < mappingList.Count)
                {
                    // Perform mapping based on the external list
                    return mappingList.ElementAt(intValue - 1).Name;
                }
            }

            return string.Empty; // Return default value if conversion is not possible
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // This converter is for one-way conversion only
        }
    }
}
