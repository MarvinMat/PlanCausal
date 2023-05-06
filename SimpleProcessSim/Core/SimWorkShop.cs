using SimSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessSim.Implementation.Core
{
    ///  <summary>
    /// deprecated
    /// </summary>
    public sealed class SimWorkShop
    {
        private static readonly Lazy<SimWorkShop> lazy = new Lazy<SimWorkShop>(() => new SimWorkShop());

        public static SimWorkShop Instance { get { return lazy.Value; } }

        public Dictionary<Guid, object> Resources { get; set; }

        private SimWorkShop()
        {
            Resources = new Dictionary<Guid, object>();
        }
    }
}
