using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.DB;

using NuGet.Protocol.Core.Types;

namespace Parkour.DB
{
    public class ParkourRankManger
    {
        private IDbConnection _database;

        public const string TableName = "ParkourRank";

        public ParkourRankManger(IDbConnection db)
        {
            _database = db;
            var table = new SqlTable(TableName,
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("AccountName", MySqlDbType.VarChar, 32),
                new SqlColumn("ParkourName",MySqlDbType.VarChar, 32),
                new SqlColumn("DeadTimes", MySqlDbType.Int32),
                new SqlColumn("Time", MySqlDbType.VarChar)) ;
            IQueryBuilder builder = db.GetSqlType() == SqlType.Sqlite
                ? (IQueryBuilder)new SqliteQueryCreator()
                : new MysqlQueryCreator();
            var sql = builder.CreateTable(table);
            var creator = new SqlTableCreator(_database, builder);
            creator.EnsureTableStructure(table);
        }

        public void CreateNewRecord(string Name,int DeadTimes,string time)
        {
            int row = _database.Query($"INSERT INTO {TableName} (AccountName,DeadTimes,Time) VALUES (@0,@1,@2)", Name,DeadTimes,time);
        }
        public string GetPlayerRecord(string Name) 
        {
            var ret = string.Empty;
            using var reader = _database.QueryReader($"SELECT * FROM {TableName} WHERE AccountName={Name}");
            while (reader.Read())
            {
                var ParkourName = reader.Get<string>("ParkourName");
                var DeadTimes = reader.Get<int>("DeadTimes");
                var Time = reader.Get<string>("Time");
                ret = $"跑酷<{ParkourName}>" +
                    $"\n玩家:{Name}" +
                    $"\n死亡次数:{DeadTimes}" +
                    $"\n耗时:{Time}";
            }
            return ret;
        }
    }
}
