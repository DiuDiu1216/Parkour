using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using System.IO.Streams;
using IL.Terraria.ID;
using Terraria.ID;
using System.Diagnostics;

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

        public ConfigFile Config { get; set; }

        public Parkour(Main game) : base(game)
        {
            Config = new ConfigFile();
            ParkourPlayers = new ParkourPlayer[256];
        }
        public enum EditStatus
        {
            StartPointWaitForSet,
            EndPointWaitForSet,
            SpawnPointWaitForSet,
            none
        }
        public Dictionary<int, (string, EditStatus)> EditDict = new Dictionary<int, (string, EditStatus)>();



        public System.Timers.Timer timer;
        public int ticks = 0;
        public int TimerPlayerIndex = -1;
        public List<int> TimerPlayers = new List<int>();
        
        
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
            timer.Elapsed += Timer_Elapsed;

            GeneralHooks.ReloadEvent += OnReload;

            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnNetGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            Commands.ChatCommands.Add(new Command(new List<string>() { "Parkour.admin" }, Parkour_Admin_Command, "pke" ));
            Commands.ChatCommands.Add(new Command(new List<string>() { "Parkour.exit" }, Parkour_Exit_Command, "退出跑酷"));
        }

        #region OnTimerEvent
        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var user = TShock.Players[TimerPlayerIndex];
            if (ticks == 30)
            {
                EditDict.Remove(TimerPlayerIndex);
                user.SendInfoMessage("跑酷指令超时");
                timer.Stop();
                timer.Dispose();
            }
            else { ticks++; }
            
        }
        #endregion


        #region Command
        private void Parkour_Exit_Command(CommandArgs args)
        {
            
        }
        private void Parkour_Admin_Command(CommandArgs args)
        {
            switch (args.Parameters[0])
            {
                case "添加开始点":
                case "addst":
                    {
                        
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("指令格式有误，正确格式:/pke 添加开始点 <跑酷名称>");
                        }
                        else
                        {
                            EditDict.Add(args.Player.Index, (args.Parameters[1], EditStatus.StartPointWaitForSet));
                            args.Player.SendMessage("请触发开始点压力板来添加开始点", Color.DodgerBlue);
                        }
                    }
                    break;
            }
        }
        #endregion


        private void OnNetGreetPlayer(GreetPlayerEventArgs args)
        {
            
            ParkourPlayers[args.Who] = new ParkourPlayer(args.Who);
        }
        private void OnServerLeave(LeaveEventArgs args)
        {
            //ParkourPlayers[args.Who].Stopwatch.Stop();
            ParkourPlayers[args.Who] = null;
            EditDict.Remove(args.Who);
            
        }



        private void OnNetGetData(GetDataEventArgs args)
        {
            TSPlayer tsplayer = TShock.Players[args.Msg.whoAmI];
            var pkplr = ParkourPlayers[args.Msg.whoAmI];
            var tsplr = TShock.Players[args.Msg.whoAmI];

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
                                                    EditDict.Remove(args.Msg.whoAmI);
                                                }
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                            EditDict.Remove(args.Msg.whoAmI);
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
                                                EditDict.Remove(args.Msg.whoAmI);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                        EditDict.Remove(args.Msg.whoAmI);
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
                                                EditDict.Remove(args.Msg.whoAmI);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        tsplayer.SendErrorMessage("添加过程中出现了神秘错误，请重新输入指令再尝试");
                                        EditDict.Remove(args.Msg.whoAmI);
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
                                                }
                                                break;
                                            case "结束点":
                                                {

                                                }
                                                break;
                                            case "重生点":
                                                {

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
                tsplr.SendMessage($"你已死亡{pkplr.DeadTimes}", Color.DodgerBlue);
                
            }
            if(args.MsgID == PacketTypes.PlayerSpawn)
            {
                if (!pkplr.IsParkouring) return;
            }


        }

        private void OnReload(ReloadEventArgs e)
        {
            RC();
        }
    }

}

