using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.DB;
using Terraria.GameContent;
using NuGet.Protocol;
using System.Net;

namespace Parkour
{

    public class ConfigFile//一定要public
    {

        public static ConfigFile Read(string 路径)//给定文件进行读
        {
            ConfigFile result;
            if (!File.Exists(路径))
            {

                result = new ConfigFile
                {
                    ParkourConfig = new List<ParkourConfig>(){new ParkourConfig("test",
                    new(),
                    new(),
                    new(){ (1,1)},
                    200,
                    new(){new NetItem(1,1,1)},
                    new(){"/help"}) }



                };
            }
            else
            {
                using (var fs = new FileStream(路径, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return Read(fs);
                }
            }
            return result;


        }
        public static ConfigFile Read(Stream stream)//给定流文件进行读取
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd());
                if (配置读取 != null)
                    配置读取(cf);
                return cf;
            }
        }

        public void Write(string 路径)//给定路径进行写
        {
            using (var fs = new FileStream(路径, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)//给定流文件写
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
        public static void WriteConfig(ConfigFile config)
        {
            File.WriteAllText(Parkour.Configpath, JsonConvert.SerializeObject(config));
            try
            {
                var c = ConfigFile.Read(Parkour.Configpath);
                c.Write(Parkour.Configpath);
            }
            catch (Exception)
            {

            }
        }


        public static Action<ConfigFile> 配置读取;//定义为常量

        public List<ParkourConfig> ParkourConfig;

        
    }

    public class ParkourConfig
    {
        public string 跑酷名称 { get; set; }
        public (short, short) 开始点 { get; set; }
        public (short, short) 结束点 { get; set; }
        public List<(short, short)> 重生点 { get; set; }
        public int 玩家血量 { get; set; }
        public List<NetItem> 玩家背包 { get; set; }
        public List<string> 执行指令 { get; set; }
        public ParkourConfig(string name, (short, short) startPoint, (short, short) endPoint,List<(short, short)> spawn,int health,List<NetItem> inventory,List<string> commands)
        {
            跑酷名称 = name;
            开始点 = startPoint;
            结束点 = endPoint;
            重生点 = spawn;
            玩家血量 = health;
            玩家背包 = inventory;
            执行指令 = commands;
        }
        
        public static ParkourConfig GetParkourByStartPoint((short, short) startPoint)
        {
            var c = ConfigFile.Read(Parkour.Configpath);
            ParkourConfig pc =null;
            for(int i = 0; i < c.ParkourConfig.Count; i++)
            {
                if (c.ParkourConfig[i].开始点 == startPoint)
                {
                    pc = c.ParkourConfig[i];
                    break;
                }
            }
            return pc;
        }
        public static ParkourConfig GetParkourByEndPoint((short, short) endPoint)
        {
            var c = ConfigFile.Read(Parkour.Configpath);
            ParkourConfig pc = null;
            for (int i = 0; i < c.ParkourConfig.Count; i++)
            {
                if (c.ParkourConfig[i].结束点 == endPoint)
                {
                    pc = c.ParkourConfig[i];
                    break;
                }
            }
            return pc;
        }
        public static ParkourConfig GerParkourByName(string name)
        {
            var c = ConfigFile.Read(Parkour.Configpath);
            ParkourConfig pc = null;
            for (int i = 0; i < c.ParkourConfig.Count; i++)
            {
                if (c.ParkourConfig[i].跑酷名称 == name)
                {
                    pc = c.ParkourConfig[i];
                    break;
                }
            }
            return pc;
        }
        public static string GetPointType((short, short) Point)
        {
            var c = ConfigFile.Read(Parkour.Configpath);
            string? type = null;
            for (int i = 0; i < c.ParkourConfig.Count; i++)
            {
                if (c.ParkourConfig[i].结束点 == Point)
                {
                    type = "结束点";
                    break;
                }
                else if (c.ParkourConfig[i].开始点 == Point)
                {
                    type = "开始点";
                    break;
                }
                else if (c.ParkourConfig[i].重生点.Contains(Point))
                {
                    type = "重生点";
                    break;
                }
            }
            return type;
        }






    }
    
    




}


