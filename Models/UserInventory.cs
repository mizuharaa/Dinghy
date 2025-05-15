using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dinghy.Models
{
    internal class UserInventory
    {
        public ulong UserId { get; set; }

        // Roll-related data
        public Dictionary<string, int> Auras { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Potions { get; set; } = new Dictionary<string, int>();

        // Gear & Equipment
        public List<string> OwnedGear { get; set; } = new List<string>();
        public string EquippedCape { get; set; }
        public string EquippedGlove { get; set; }

        // Luck Modifiers
        public double BaseLuckMultiplier { get; set; } = 1.0;
        public DateTime? PotionEndTime { get; set; }
        public string ActivePotionType { get; set; } // e.g., Lucky Potion I, etc.
        public string PendingOneTimePotion { get; set; } // Heavenly, Oblivion, ??? Potions
    }
}
