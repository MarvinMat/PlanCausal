using Core.Abstraction.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimulatorConfigurator.Services.Provider
{
    public class DataProvider
    {
        public static List<T> Load<T>(string path) 
        {
            string json = File.ReadAllText(path, Encoding.UTF8);

            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
    }
}
