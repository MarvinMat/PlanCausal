using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Abstraction.Domain.Enums
{

    public enum OrderState
    {
        Created,
        Pending,
        InProgress,
        Completed,
        Cancelled

    }
}
