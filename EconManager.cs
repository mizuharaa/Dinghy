using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;


namespace Dinghy
{
    public class EconManager
    {
        private const string FilePath = "economy.json";

        private class UserEconomyData
        {
            public long Balance { get; set; } = 0;

            [JsonProperty]
            public DateTime LastDailyClaim { get; set; } = DateTime.MinValue;
        }

        private Dictionary<ulong, UserEconomyData> balances = new Dictionary<ulong, UserEconomyData>();

        public EconManager()
        {
            ViewBalance();
        }

        private void ViewBalance()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                balances = JsonConvert.DeserializeObject<Dictionary<ulong, UserEconomyData>>(json)
                           ?? new Dictionary<ulong, UserEconomyData>();
            }
        }

        private void SaveBalance()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(balances, Formatting.Indented));
        }

        public long GetBalance(ulong UserID)
        {
            return balances.TryGetValue(UserID, out var data) ? data.Balance : 0;
        }

        public void LoadBalance(ulong userID, long amount)
        {
            if (!balances.ContainsKey(userID))
                balances[userID] = new UserEconomyData();

            balances[userID].Balance += amount;
            SaveBalance();
        }

        public bool CanClaimDaily(ulong userID, out TimeSpan remaining)
        {
            if (!balances.ContainsKey(userID))
            {
                balances[userID] = new UserEconomyData();
                remaining = TimeSpan.Zero;
                return true;
            }

            DateTime lastClaim = balances[userID].LastDailyClaim;
            DateTime now = DateTime.UtcNow;

            if ((now - lastClaim) >= TimeSpan.FromHours(24))
            {
                remaining = TimeSpan.Zero;
                return true;
            }
            else
            {
                remaining = TimeSpan.FromHours(24) - (now - lastClaim);
                return false;
            }
        }

        public void UpdateLastClaim(ulong userID)
        {
            if (!balances.ContainsKey(userID))
                balances[userID] = new UserEconomyData();

            balances[userID].LastDailyClaim = DateTime.UtcNow;
            SaveBalance();
        }
    }
}
