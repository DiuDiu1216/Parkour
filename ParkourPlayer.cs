using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using System.IO.Streams;
using IL.Terraria.ID;
using Terraria.ID;
using System.Diagnostics;
using Parkour.DB;

namespace Parkour
{
    public class ParkourPlayer
    {
       
        public int Who { get; set; }
        public string Name { get; set; }
        public bool IsParkouring { get; set; }
        public string? CurrentParkour { get; set; }
        public (short,short) SpawnPoint { get; set; }
        public int DeadTimes { get; set; }
        public Stopwatch? Stopwatch { get; set; }
        public Item[] Inventory { get; set; }
        public ParkourPlayer(int who,string name,bool isparkouring,string currentParkour, (short, short)spawnpoint,int deadtimes, Stopwatch stopwatch)
        {
            
            Who = who;
            Name = name;
            IsParkouring = isparkouring;
            CurrentParkour = currentParkour;
            DeadTimes = deadtimes;
            SpawnPoint = spawnpoint;
            DeadTimes = deadtimes;
            Stopwatch = stopwatch;

        }
        public ParkourPlayer(int who)
        {
            Who = who;
        }
        public ParkourPlayer() { }
        
        
        


        


    }
}
