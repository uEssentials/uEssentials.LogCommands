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
using System.Diagnostics;
using System.IO;
using System.Text;
using Essentials.Api.Configuration;
using Essentials.Api.Event;
using Essentials.Api.Events;
using Essentials.Api.Module;
using Essentials.Api.Task;
using Essentials.Common;

namespace Essentials.Modules.LogCommands {

    [ModuleInfo(
        Name = "uEssentials.LogCommands",
        Author = "Leonardosnt",
        Version = "$ASM_VERSION",
        Flags = LoadFlags.AUTO_REGISTER_EVENTS
    )]
    public class LogCommands : EssModule<LogCommandsConfig> {

        public static Dictionary<ulong, List<string>> Cache { get; } = new Dictionary<ulong, List<string>>();

        public string LogFolder {
            get {
                var path = Path.Combine(Folder, "logs");
                if (!Directory.Exists(path)) {
                  Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public override void OnLoad() {
            Logger.LogInfo($"Enabled (v{this.Info.Version})!");

            Task.Create()
              .Id("LogCommands Save Cache")
              .Async()
              .Action(SaveCache)
              .Interval(TimeSpan.FromSeconds(Configuration.SaveInterval))
              .UseIntervalAsDelay()
              .Submit();
        }

        public override void OnUnload() {
            Logger.LogInfo($"Disabled (v{this.Info.Version})!");
            SaveCache();
        }

        [SubscribeEvent(EventType.ESSENTIALS_COMMAND_POS_EXECUTED)]
        private void OnCommandExecuted(CommandPosExecuteEvent e) {
            if (e.Source.IsConsole) return;

            var playerId = ulong.Parse(e.Source.Id);
            var sb = new StringBuilder();

            sb.Append($"[{DateTime.Now}] ")
                .Append($"[{e.Result.Type}")
                .Append(string.IsNullOrEmpty(e.Result.Message) ? "] " : $"({e.Result.Message})] ")
                .Append(e.Source.DisplayName)
                .Append(": ")
                .Append($"\"/{e.Command.Name}")
                .Append(e.Arguments.IsEmpty ? "\"" : $" {e.Arguments.Join(0)}\"");

            var text = sb.ToString();
            if (Cache.ContainsKey(playerId)) {
              Cache[playerId].Add(text);
            } else {
              Cache.Add(playerId, new List<string> { text });
            }
        }

        private void SaveCache() {
            lock (Cache) {
#if DEBUG
              var sw = Stopwatch.StartNew();
#endif
              var text = new StringBuilder();
                Cache.ForEach((k) => {
                  var playerFolder = Path.Combine(LogFolder, k.Key.ToString());
                  var commandsFile = Path.Combine(playerFolder, "commands.txt");

                  if (!Directory.Exists(playerFolder))
                    Directory.CreateDirectory(playerFolder);

                  if (!File.Exists(commandsFile))
                    File.Create(commandsFile).Close();

                  k.Value.ForEach(t => text.Append(t).Append(Environment.NewLine));
                  File.AppendAllText(commandsFile, text.ToString());
                });

                Cache.Clear();
#if DEBUG
                sw.Stop();
                Logger.LogDebug($"Save cache took: {sw.ElapsedMilliseconds}ms");
#endif
            }
        }

    }

    public class LogCommandsConfig : JsonConfig {

        /// <summary>
        /// Interval to save cache. In seconds.
        /// </summary>
        public int SaveInterval { get; set; }

        public override void LoadDefaults() {
#if DEBUG
            SaveInterval = 10;
#else
            SaveInterval = 60; // 2min
#endif
        }

    }

}