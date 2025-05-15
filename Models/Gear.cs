using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinghy.Models
{
    internal class Gear
    {
        public string Name { get; set; }
        public string Type { get; set; } // e.g., "Glove", "Cape"
        public double EffectMultiplier { get; set; }
    }
}
