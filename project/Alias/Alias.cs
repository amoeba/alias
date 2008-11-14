using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace ACPop
{
    [FriendlyName("Alias")]
    [WireUpBaseEvents]
    public partial class PluginCore : PluginBase
    {
        public bool enabled = true;

        Hashtable Aliases = new Hashtable();

        Regex regex;
        Match matches;

        string commandPrefix;
        string settingsFileName;

        protected override void Startup()
        {
            try
            {
                regex = new Regex(@"(?<pre>\[\w+\] )?(?<player>.*)(?<post> (says|tells you).*)");
                commandPrefix = "alias";
                settingsFileName = "settings.txt";
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        protected override void Shutdown()
        {
            try
            {
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        [BaseEvent("LoginComplete")]
        private void OnLoginComplete(object sender, EventArgs e)
        {
            LoadSettings();
        }

        [BaseEvent("ChatBoxMessage")]
        private void OnChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if (!enabled)
                return;

            e.Eat = true;

            string output = e.Text;
            matches = regex.Match(output);

            if (matches.Success)
            {   
                string player = matches.Groups["player"].ToString();
                string player_name = Regex.Match(player, @">(.*)<").Groups[1].ToString();

                if (Aliases.ContainsKey(player_name))
                {
                    output = matches.Groups["pre"] + "<" + Aliases[player_name] + "> " + matches.Groups["player"] + matches.Groups["post"];
                }
            }

            WriteToChat(output, e.Color, e.Target);
        }

        [BaseEvent("CommandLineText")]
        private void OnCommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            string command = e.Text;

            if (command.StartsWith("@") || command.StartsWith("/"))
            {
                // Strip leading @ or /
                command = command.Substring(1);

                if (command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string action = command.Substring(commandPrefix.Length + 1);

                    if (action.StartsWith("enable"))
                    {
                        if (!enabled)
                        {
                            enabled = true;
                            WriteToChat("Alias has been enabled.");
                        }
                        else
                        {
                            WriteToChat("Alias is already enabled.");
                        }
                    }
                    else if (action.StartsWith("disable"))
                    {
                        if (enabled)
                        {
                            enabled = false;
                            WriteToChat("Alias has been disabled.");
                        }
                        else
                        {
                            WriteToChat("Alias is already disabled.");
                        }
                    }                        
                    else if(action.StartsWith("add"))
                    {
                        Match m = Regex.Match(action, @"add ""(?<first>.*)"" ""(?<second>.*)""");

                        if (m.Success)
                        {
                            Aliases.Add(m.Groups["first"].ToString(), m.Groups["second"].ToString());
                            WriteToChat(m.Groups["first"].ToString() + " has been aliased as " + m.Groups["second"].ToString());
                            SaveSettings();
                        }
                        else
                        {
                            WriteToChat("Couldn't parse @alias add command.");
                        }
                    }
                    else if(action.StartsWith("remove"))
                    {
                        string player = action.Substring(7);

                        if (Aliases.ContainsKey(player))
                        {
                            Aliases.Remove(player);
                            WriteToChat(player + " successfully removed from Alias list");
                            SaveSettings();
                        }
                        else
                        {
                            WriteToChat("Couldn't find " + player + " in Alias list");
                        }
                    }
                    else if(action.StartsWith("list"))
                    {
                        if (Aliases.Count == 0)
                        {
                            WriteToChat("You have not set any aliases yet.");
                        }
                        else
                        {
                            WriteToChat(Aliases.Count + " aliases total:");

                            foreach(string key in Aliases.Keys)
                            {
                                WriteToChat(key + " => " + Aliases[key]);
                            }
                        }
                    }
                    else if (action.StartsWith("help"))
                    {
                        WriteToChat("Alias v0.1", 7);
                        WriteToChat("By Kolthar (petridish@gmail.com)", 7);
                        WriteToChat("Available commands: enable, disable, add, remove, list, help", 7);
                    }
                    else
                    {
                        WriteToChat("For help with Alias, type `@alias help`");
                    }
                }
            }
        }

        public void WriteToChat(string msg)
        {
            Host.Actions.AddChatText(msg, 1);
        }

        public void WriteToChat(string msg, int color)
        {
            Host.Actions.AddChatText(msg, color);
        }

        public void WriteToChat(string msg, int color, int target)
        {
            Host.Actions.AddChatText(msg, color, target);
        }

        private void HandleException(Exception ex)
        {
            WriteToChat("Exception thrown for " + ex.Source);
            WriteToChat(ex.Message);
            WriteToChat(ex.StackTrace);
        }

        private void LoadSettings()
        {
            FileInfo settingsFile = new FileInfo(fullPath(settingsFileName));

            if (settingsFile.Exists)
            {
                using (StreamReader sr = new StreamReader(fullPath(settingsFileName), true))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] kvp = line.Split(new char[] { ',' });

                        if (kvp.Length == 2)
                        {
                            Aliases[kvp[0]] = kvp[1];
                        }
                    }
                }
            }

            WriteToChat("Settings successfully loaded.");
        }

        private void SaveSettings()
        {
            if (Aliases.Count > 0)
            {
                FileInfo settingsFile = new FileInfo(fullPath(settingsFileName));

                if (settingsFile.Exists)
                    settingsFile.Delete();

                using (StreamWriter sw = new StreamWriter(fullPath(settingsFileName), true))
                {
                    ICollection keys = Aliases.Keys;

                    foreach (string key in keys)
                    {
                        sw.WriteLine(key + "," + Aliases[key]);
                    }
                }
            }
        }

        private string fullPath(string filename)
        {
            string basePath = System.Reflection.Assembly.GetExecutingAssembly().Location.ToString();

            int slash = basePath.LastIndexOf('\\');

            if (slash > 0)
                basePath = basePath.Substring(0, slash + 1);

            return System.IO.Path.Combine(basePath, filename);
        }
    }
}