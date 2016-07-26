/*
 *  This file is part of uEssentials project.
 *      https://uessentials.github.io/
 *
 *  Copyright (C) 2015-2016  Leonardosc
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Essentials.Api.Event;
using Essentials.Api.Events;
using Essentials.Api.Module;
using Essentials.Common;

namespace Essentials.Modules.LogCommands
{
    [ModuleInfo(
        Name = "uEssentials.LogCommands",
        Author = "Leonardosc",
        Version = "1.0.2",
        Flags = LoadFlags.AUTO_REGISTER_EVENTS
    )]
    public class LogCommands : EssModule
    {
        public static Dictionary<ulong, List<string>> Cache { get; } = new Dictionary<ulong, List<string>>();
        public static LogCommands Instance { get; private set; }
        private int _checks;

        public string LogFolder
        {
            get
            {
                var path = Path.Combine(Folder, "logs");
                if ( !Directory.Exists(path) )
                    Directory.CreateDirectory( path );
                return path;
            }
        }

        public override void OnLoad()
        {
            Logger.LogInfo( $"Enabled (v{this.Info.Version})!" );
            Instance = this;
        }

        public override void OnUnload()
        {
            Logger.LogInfo( $"Disabled (v{this.Info.Version})!" );
            SaveCache();
        }

        [SubscribeEvent( EventType.ESSENTIALS_COMMAND_POS_EXECUTED )]
        private void OnCommandExecuted( CommandPosExecuteEvent e )
        {
            if ( e.Source.IsConsole ) return;

            // TODO: Improve saving...
            if ( Cache.Count >= 15 || (_checks > 20 && CheckValues()) )
            {
                SaveCache();
                _checks = 0;
            }
            else
            {
                _checks++;
            }

            var playerId = ulong.Parse(e.Source.Id);
            var sb = new StringBuilder();

            sb.Append( $"[{DateTime.Now}] " )
              .Append( $"[{e.Result.Type}" )
              .Append( string.IsNullOrEmpty(e.Result.Message) ? "] " : $"({e.Result.Message})] " )
              .Append( e.Source.DisplayName )
              .Append( ": " )
              .Append( $"\"/{e.Command.Name}" )
              .Append( e.Arguments.IsEmpty ? "\"" : $" {e.Arguments.Join(0)}\"" );

            var text = sb.ToString();

            if ( Cache.ContainsKey( playerId ) )
                Cache[playerId].Add( text );
            else
                Cache.Add( playerId, new List<string>{ text } );

            #if DEBUG
              SaveCache();
            #endif
        }

        private void SaveCache()
        {
            var copyOfCache = new Dictionary<ulong, List<String>>(Cache);
            Cache.Clear();

            new Thread(() => {
                var text = new StringBuilder();
                copyOfCache.ForEach((k) => {
                    var playerFolder = Path.Combine(LogFolder, k.Key.ToString());
                    var commandsFile = Path.Combine( playerFolder, "commands.txt" );

                    if ( !Directory.Exists( playerFolder ) )
                        Directory.CreateDirectory( playerFolder );

                    if ( !File.Exists( commandsFile ) )
                        File.Create( commandsFile ).Close();

                    k.Value.ForEach(t => text.Append( t ).Append( Environment.NewLine ) );
                    File.AppendAllText( commandsFile, text.ToString() );
                });
            }).Start();
        }

        /*
            Sum all commands in cache, return true if > 100
        */
        private bool CheckValues()
        {
            return Cache.Values.Sum(a => a.Count) > 100;
        }
    }
}
