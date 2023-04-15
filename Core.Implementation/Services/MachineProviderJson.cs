using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using System.Diagnostics;
using System.Text.Json;


namespace Core.Implementation.Services
{
    public class MachinProviderJson : IMachineProvider
    {
        private readonly string _path;
        public MachinProviderJson(string path)
        {
            _path = path;
        }
        public IEnumerable<Machine> Load()
        {
            try
            {
                string json = File.ReadAllText(_path);

                var machineTypes = JsonSerializer.Deserialize<IEnumerable<MachineTypeVO>>(json);

                if (machineTypes == null)
                {
                    Debug.WriteLine("Empty machine list");
                    return new List<Machine>();
                }

                var machines = new List<Machine>();
                machineTypes.ToList().ForEach(machineType =>
                {
                    for (var i = 0; i < machineType.Count; i++)
                    {
                        machines.Add(new Machine { Name = machineType.Name, MachineType = machineType.MachineTypeId });
                    }
                });

                return machines;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to deserialize machines. {ex}");
            }
        }
    }

}
