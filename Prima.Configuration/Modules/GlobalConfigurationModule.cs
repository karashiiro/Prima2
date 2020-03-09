using System;
using 命令 = Discord.Commands.CommandAttribute;
using ソケットなモジュールベース = Discord.Commands.ModuleBase<Discord.Commands.SocketCommandContext>;
using 名前Attribute = Discord.Commands.NameAttribute;
using 一遍アドミンAttribute = Discord.Commands.RequireOwnerAttribute;
using モードエグゼキュート = Discord.Commands.RunMode;
using データベース = Prima.Services.DbService;
using ストリング = System.String;
using タスク = System.Threading.Tasks.Task;

namespace Prima.Configuration.Modules
{
    /// <summary>
    /// このモジュールは大域的な設定を持って、一遍アドミンを使えます。
    /// </summary>
    [名前("大域的な設定")]
    [一遍アドミン]
    public class 大域的な設定のモジュール : ソケットなモジュールベース
    {
        public データベース Db { get; set; }

        // プリーマの大域的な設定をします。
        [命令("設定", RunMode = モードエグゼキュート.Async)]
        public async タスク 設定するAsync(ストリング キー, ストリング ヴァエル)
        {
            try
            {
                await Db.SetGlobalConfigurationProperty(キー, ヴァエル);
                await ReplyAsync("Property updated. Please verify your global configuration change.");
            }
            catch (ArgumentException e)
            {
                await ReplyAsync($"Error: {e.Message}");
            }
        }
    }
}
