using Core.Abstraction.Domain.Models;
using Core.Abstraction.Domain.Resources;
using Core.Abstraction.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;


namespace Core.Implementation.Services
{
    public class MachineProviderJson : IMachineProvider
    {
        private readonly string _path;
        public MachineProviderJson(string path)
        {
            _path = path;
        }
        public List<Machine> Load()
        {
            try
            {
                string json = File.ReadAllText(_path, Encoding.UTF8);

                var machineTypes = JsonSerializer.Deserialize<List<MachineTypeVO>>(json);

                if (machineTypes == null)
                {
                    throw new Exception($"Deserialization returned null.");
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
