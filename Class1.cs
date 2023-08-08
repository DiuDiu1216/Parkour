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
    [ApiVersion(2, 1)]
    public class Parkour : TerrariaPlugin
    {
        public override string Name => "Parkour";

        public override Version Version => new Version(1, 0, 0, 0);

        public override string Author => "diudiu";

        public override string Description => "跑酷";

        public static string Configpath
        {
            get
            {
                return Path.Combine(TShock.SavePath, "Parkour.json");
            }
        }

        public ParkourPlayer[] ParkourPlayers { get; set; }

        public ParkourRankManger ParkourRankManger { get; private set; }

        public ConfigFile Config { get; set; }

        public Parkour(Main game) : base(game)
        {
            Config = new ConfigFile();
            ParkourPlayers = new ParkourPlayer[Main.maxPlayers];
        }
        public enum EditStatus
        {
            StartPointWaitForSet,
            EndPointWaitForSet,
            SpawnPointWaitForSet,
            none
        }
        //key为玩家index，value.item1为ParkourName
        public Dictionary<int, (string, EditStatus)> EditDict = new Dictionary<int, (string, EditStatus)>();

        

      
        
        public void RC()
        {
            try
            {
                bool flag = !File.Exists(Configpath);
                if (flag)
                {
                    TShock.Log.ConsoleError("未找到Parkour配置，已为您创建！");
                }
                Config = ConfigFile.Read(Configpath);
                Config.Write(Configpath);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("Parkour配置读取错误:" + ex.ToString());
            }
        }

        public override void Initialize()
        {
            RC();
            

            ParkourRankManger = new ParkourRankManger(TShock.DB);

            GeneralHooks.ReloadEvent += OnReload;
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnNetGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            Commands.ChatCommands.Add(new Command(new List<string>() { "Parkour.admin" }, Parkour_Admin_Command, "pke" ));
            Commands.ChatCommands.Add(new Command(new List<string>() { "Parkour.exit" }, Parkour_Exit_Command, "退出跑酷"));
        }

        
        #region Command
        private void Parkour_Exit_Command(CommandArgs args)
        {
            args.Player.Teleport(Main.spawnTileX*16, Main.spawnTileY*16);
            var pkplr = ParkourPlayers[args.Player.Index];
            var tsplayer = args.Player;
            var inventory = pkplr.Inventory;
            for (var i = 0; i < pkplr.Inventory.Length; i++)
            {

                tsplayer.TPlayer.inventory[i].netID = inventory[i].netID;
                tsplayer.TPlayer.inventory[i].stack = inventory[i].stack;
                tsplayer.TPlayer.inventory[i].prefix = inventory[i].prefix;
                NetMessage.SendData(5, -1, -1, null, tsplayer.Index, i);
            }
            ParkourPlayers[args.Player.Index] = new ParkourPlayer();

            args.Player.SendMessage("你已成功退出跑酷", Color.DodgerBlue);
        }
        private void Parkour_Admin_Command(CommandArgs args)
        {
            switch (args.Parameters[0])
            {
                case "添加开始点":
                case "adds":
                    {
                        
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("指令格式有误，正确格式:/pke 添加开始点 <跑酷名称>");
                        }
                        else
                        {
                            if (EditDict.ContainsKey(args.Player.Index))
                            {
                                EditDict[args.Player.Index] = (args.Parameters[1], EditStatus.StartPointWaitForSet);
                            }
                            else
                            {
                                EditDict.Add(args.Player.Index, (args.Parameters[1], EditStatus.StartPointWaitForSet));
                            }
                            args.Player.SendMessage("请触发开始点压力板来添加开始点", Color.DodgerBlue);
                        }
                    }
                    break;
                case "添加结束点":
                case "added":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("指令格式有误，正确格式:/pke 添加结束点 <跑酷名称>");
                        }
                        else
                        {
                            if (EditDict.ContainsKey(args.Player.Index))
                            {
                                EditDict[args.Player.Index] = (args.Parameters[1], EditStatus.EndPointWaitForSet);
                            }
                            else
                            {
                                EditDict.Add(args.Player.Index, (args.Parameters[1], EditStatus.EndPointWaitForSet));
                            }
                            
                            args.Player.SendMessage("请触发开始点压力板来添加结束点", Color.DodgerBlue);
                        }
                    }
                    break;
                case "添加重生点":
                case "addrs":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("指令格式有误，正确格式:/pke 添加重生点 <跑酷名称>");
                        }
                        else
                        {
                            if (EditDict.ContainsKey(args.Player.Index))
                            {
                                EditDict[args.Player.Index] = (args.Parameters[1], EditStatus.SpawnPointWaitForSet);
                            }
                            else
                            {
                                EditDict.Add(args.Player.Index, (args.Parameters[1], EditStatus.SpawnPointWaitForSet));
                            }
                            args.Player.SendMessage("请触发开始点压力板来添加重生点", Color.DodgerBlue);
                        }
                    }
                    break;
               /* case "创建跑酷":
                case "new":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("指令格式有误，正确格式:/pke 创建跑酷 <跑酷名称>");
                        }
                        else
                        {
                            try
                            {
                                Config.ParkourConfig.Add(new ParkourConfig(args.Parameters[1]));
                                ConfigFile.WriteConfig(Config);
                                args.Player.SendMessage($"<{args.Parameters[1]}>已创建", Color.DodgerBlue);
                            }
                            catch (Exception ex)
                            {
                                args.Player.SendErrorMessage(ex.ToString());
                            }
                            
                        }
                    }
                    break;*/
                    
            }   
        }
        #endregion


        private void OnNetGreetPlayer(GreetPlayerEventArgs args)
        {
            ParkourPlayers[args.Who] = new ParkourPlayer(args.Who);
            EditDict.Add(args.Who, ("", EditStatus.none));
        }
        private void OnServerLeave(LeaveEventArgs args)
        {
            //ParkourPlayers[args.Who].Stopwatch.Stop();
            if (ParkourPlayers[args.Who] == null) return;
            ParkourPlayers[args.Who] = null;
            EditDict.Remove(args.Who);
            
        }



        private void OnNetGetData(GetDataEventArgs args)
        {
            TSPlayer tsplayer = TShock.Players[args.Msg.whoAmI];
            var pkplr = ParkourPlayers[args.Msg.whoAmI];
            var tsplr = TShock.Players[args.Msg.whoAmI];
            if(pkplr == null) return;   
            #region PressurePlate
            if (EditDict.ContainsKey(args.Msg.whoAmI))
            {
                var command_type = EditDict[args.Msg.whoAmI].Item2;
                var OperatingParkour = EditDict[args.Msg.whoAmI].Item1;
                switch (command_type)
                {
                    case EditStatus.StartPointWaitForSet:
                        {
                            if (args.MsgID == PacketTypes.HitSwitch)
                            {
                                //TShock.Utils.Broadcast("hit", Color.DodgerBlue);
                                using (var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                                {
                                    var TriggerPos = (stream.ReadInt16(), stream.ReadInt16());
                                    var tile = Main.tile[TriggerPos.Item1, TriggerPos.Item2];
                                    if (tile.type == Terraria.ID.TileID.PressurePlates)
                                    {
                                        //TShock.Utils.Broadcast("PressurePlates", Color.DodgerBlue);
                                        try
                                        {
                                            for (var i = 0; i < Config.ParkourConfig.Count; i++)
                                            {
                                                if (Config.ParkourConfig[i].跑酷名称 == OperatingParkour)
                                                {
                                                    Config.ParkourConfig[i].开始点 = TriggerPos;
                                                    ConfigFile.WriteConfig(Config);
                                                    tsplayer.SendMessage($"<{Config.ParkourConfig[i].跑酷名称}>成功添加开始点," +
                                                        $"坐标为<{TriggerPos.Item1},{TriggerPos.Item2}>", Color.DodgerBlue);
                                                    EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                            EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case EditStatus.EndPointWaitForSet:
                        if (args.MsgID == PacketTypes.HitSwitch)
                        {
                            using (var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                            {
                                var TriggerPos = (stream.ReadInt16(), stream.ReadInt16());
                                var tile = Main.tile[TriggerPos.Item1, TriggerPos.Item2];
                                if (tile.type == Terraria.ID.TileID.PressurePlates)
                                {
                                    try
                                    {
                                        for (var i = 0; i < Config.ParkourConfig.Count; i++)
                                        {
                                            if (Config.ParkourConfig[i].跑酷名称 == OperatingParkour)
                                            {
                                                Config.ParkourConfig[i].结束点 = TriggerPos;
                                                ConfigFile.WriteConfig(Config);
                                                tsplayer.SendMessage($"<{Config.ParkourConfig[i].跑酷名称}>成功添加结束点," +
                                                        $"坐标为<{TriggerPos.Item1},{TriggerPos.Item2}>", Color.DodgerBlue);
                                                EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                        EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                    }
                                }
                            }
                        }
                        break;
                    case EditStatus.SpawnPointWaitForSet:
                        if (args.MsgID == PacketTypes.HitSwitch)
                        {
                            using (var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                            {
                                var TriggerPos = (stream.ReadInt16(), stream.ReadInt16());
                                var tile = Main.tile[TriggerPos.Item1, TriggerPos.Item2];
                                if (tile.type == Terraria.ID.TileID.PressurePlates)
                                {
                                    try
                                    {

                                        for (var i = 0; i < Config.ParkourConfig.Count; i++)
                                        {
                                            if (Config.ParkourConfig[i].跑酷名称 == OperatingParkour)
                                            {
                                                if (Config.ParkourConfig[i].重生点.Contains(TriggerPos))
                                                {
                                                    tsplayer.SendErrorMessage("添加点重复，请重新输入指令再添加");
                                                    return;
                                                }
                                                Config.ParkourConfig[i].重生点.Add(TriggerPos);
                                                ConfigFile.WriteConfig(Config);
                                                tsplayer.SendMessage($"<{Config.ParkourConfig[i].跑酷名称}>成功添加新重生点," +
                                                        $"坐标为<{TriggerPos.Item1},{TriggerPos.Item2}>", Color.DodgerBlue);
                                                EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                        EditDict[args.Msg.whoAmI] = ("", EditStatus.none);
                                    }
                                }
                            }
                        }
                        break;
                    case EditStatus.none:
                        {
                            if (args.MsgID == PacketTypes.HitSwitch)
                            {
                                using (var stream = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                                {
                                    var TriggerPos = (stream.ReadInt16(), stream.ReadInt16());
                                    var tile = Main.tile[TriggerPos.Item1, TriggerPos.Item2];
                                    if (tile.type == Terraria.ID.TileID.PressurePlates)
                                    {
                                        var type = ParkourConfig.GetPointType(TriggerPos);
                                        
                                        if(type == null) { return; }
                                        switch (type)
                                        {
                                            case "开始点":
                                                {
                                                    var pc = ParkourConfig.GetParkourByStartPoint(TriggerPos);
                                                    if (pc == null) { return; }
                                                    

                                                    pkplr.CurrentParkour = pc.跑酷名称;
                                                    pkplr.Name = tsplr.Name;
                                                    pkplr.IsParkouring = true;
                                                    pkplr.Stopwatch = new();
                                                    pkplr.DeadTimes = 0;
                                                    pkplr.SpawnPoint = pc.开始点;
                                                    pkplr.Stopwatch.Start();
                                                    pkplr.Inventory = tsplayer.TPlayer.inventory;
                                                    for(var i=0; i < tsplayer.TPlayer.inventory.Length; i++)
                                                    {
                                                        var inventory = ParkourConfig.GerParkourByName(pc.跑酷名称).玩家背包; 
                                                        if (i <= inventory.Count - 1)
                                                        {
                                                            tsplayer.TPlayer.inventory[i].netID = inventory[i].NetId;
                                                            tsplayer.TPlayer.inventory[i].stack = inventory[i].Stack;
                                                            tsplayer.TPlayer.inventory[i].prefix = inventory[i].PrefixId;
                                                            NetMessage.SendData(5, -1, -1, null, tsplayer.Index, i);
                                                        }
                                                        else
                                                        {
                                                            tsplayer.TPlayer.inventory[i].netID = 0;
                                                            NetMessage.SendData(5, -1, -1, null, tsplayer.Index, i);
                                                        }
                                                        
                                                        
                                                    }
                                                    
                                                }
                                                break;
                                            case "结束点":
                                                {
                                                    var pc = ParkourConfig.GetParkourByEndPoint(TriggerPos);
                                                    if (pc == null) {
                                                        tsplayer.SendInfoMessage("Error:GetParkourByEndPoint");
                                                        return; 
                                                    }
                                                    
                                                    if(pc.跑酷名称 != pkplr.CurrentParkour && !pkplr.IsParkouring && pkplr.Stopwatch!=null)
                                                    {
                                                        return;
                                                    }
                                                    var inventory = pkplr.Inventory;
                                                    for (var i = 0; i < pkplr.Inventory.Length; i++)
                                                    {
                                                        
                                                        tsplayer.TPlayer.inventory[i].netID = inventory[i].netID;
                                                        tsplayer.TPlayer.inventory[i].stack = inventory[i].stack;
                                                        tsplayer.TPlayer.inventory[i].prefix = inventory[i].prefix;
                                                        NetMessage.SendData(5, -1, -1, null, tsplayer.Index, i);
                                                    }
                                                    var UsingTime = pkplr.Stopwatch.Elapsed.TotalSeconds;
                                                    pkplr.Stopwatch.Stop();
                                                    tsplayer.SendMessage($"你已成功完成【{pkplr.CurrentParkour}】" +
                                                        $"\n玩家:{pkplr.Name}" +
                                                        $"\n死亡次数:{pkplr.DeadTimes}" +
                                                        $"\n耗时:{UsingTime}" +
                                                        $"\n", Color.DodgerBlue);
                                                    try
                                                    {
                                                        ParkourRankManger.CreateNewRecord(pkplr.CurrentParkour, pkplr.DeadTimes, UsingTime.ToString());
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        TShock.Log.Error(ex.ToString());
                                                    }
                                                    ParkourPlayers[args.Msg.whoAmI] = new ParkourPlayer();
                                                }
                                                break;
                                            case "重生点":
                                                {
                                                    if (!pkplr.IsParkouring) return;
                                                    tsplayer.SendMessage("已将你的重生点更新到此处",Color.DodgerBlue);
                                                    pkplr.SpawnPoint = TriggerPos;
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            #endregion 


            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                if(!pkplr.IsParkouring) { return; }
                pkplr.DeadTimes += 1;
                
                tsplayer.Spawn(0,null);
              
                tsplr.SendMessage($"你已死亡{pkplr.DeadTimes}", Color.DodgerBlue);
                args.Handled = true;
            }
            if(args.MsgID == PacketTypes.PlayerSpawn)
            {
                if(!pkplr.IsParkouring) { return; }
                
                tsplayer.Teleport(pkplr.SpawnPoint.Item1*16, pkplr.SpawnPoint.Item2*16-16);
                args.Handled = true;
            }
            
        }
        private void OnReload(ReloadEventArgs e)
        {
            RC();
        }
    }
}

