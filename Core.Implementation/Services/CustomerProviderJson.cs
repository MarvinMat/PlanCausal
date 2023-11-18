using System.Text;
using System.Text.Json;
using Core.Abstraction.Domain.Customers;
using Core.Abstraction.Services;

namespace Core.Implementation.Services;

public class CustomerProviderJson : IEntityLoader<Customer>
{
    private readonly string _path;

    public CustomerProviderJson(string path)
    {
        _path = path;
    }

    public List<Customer> Load()
    {
        try
        {
            var json = File.ReadAllText(_path, Encoding.UTF8);

            var customers = JsonSerializer.Deserialize<List<Customer>>(json);

            if (customers == null)
            {
                throw new Exception($"Deserialization returned null.");
            }

            return customers;
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to deserialize customers. {ex}");
        }

    }
}
  