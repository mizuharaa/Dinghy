using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinghy.Models
{
    internal class potion
    {
        public string Name { get; set; }
        public int DurationMinutes { get; set; }
        public double LuckBoost { get; set; }
        public bool IsStackable { get; set; }
    }
}
