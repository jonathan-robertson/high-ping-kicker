using System;
using System.Collections.Generic;

namespace HighPingKicker {
    internal class ConsoleCmdHighPingKicker : ConsoleCmdAbstract {
        private static readonly string[] commands = { "highpingkicker", "hpk" };

        public override string[] GetCommands() {
            return commands;
        }

        public override string GetDescription() {
            return "Manage Ping thresholds to auto-kick/ban players exceeding the set limit";
        }

        public override string GetHelp() {
            int i = 1;
            int j = 1;
            return $@"Usage:
  {i++}. {commands[0]}
  {i++}. {commands[0]} set <Option> <Value>
  {i++}. {commands[0]} list
  {i++}. {commands[0]} reset
Description Overview
{j++}. View current configuration
{j++}. Update a key/value pair in the configuration
{j++}. Show list of players currently being tracked for high ping
{j++}. Delete the configuration file and create a new one with default values";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            try {
                switch (_params.Count) {
                    case 0:
                        SdtdConsole.Instance.Output(Service.Instance.Config.ToString());
                        return;
                    case 1:
                        if ("reset".EqualsCaseInsensitive(_params[0])) {
                            Service.Reset(SdtdConsole.Instance);
                            return;
                        }
                        if ("list".EqualsCaseInsensitive(_params[0])) {
                            if (Service.Instance.Violations.Count == 0) {
                                SdtdConsole.Instance.Output("No players are currently being watched for excessive latency.");
                                return;
                            }
                            foreach (var key in Service.Instance.Violations.Keys) {
                                SdtdConsole.Instance.Output(Service.Instance.Violations[key].ToString());
                            }
                            return;
                        }
                        break;
                    case 3:
                        if ("set".EqualsCaseInsensitive(_params[0]) && int.TryParse(_params[2], out var value)) {
                            try {
                                if (Service.Instance.Set(_params[1], value)) {
                                    SdtdConsole.Instance.Output($"Successfully updated {_params[1]} -> {value}\nUpdated config file at {Service.Path}.");
                                } else {
                                    SdtdConsole.Instance.Output($"Invalid option provided: {_params[1]}\nUse command '{commands[0]}' to view as list of options you can change.");
                                }
                            } catch (Exception e) {
                                SdtdConsole.Instance.Output($"Failed to update file at {Service.Path}.\n{e.Message}\n{e.StackTrace}");
                            }
                            return;
                        }
                        break;
                    default:
                        SdtdConsole.Instance.Output($"Invalid number of parameters. Try running 'help {commands[0]}' to get a list of options.");
                        return;
                }
                SdtdConsole.Instance.Output($"Invalid option. Try running 'help {commands[0]}' to get a list of options.");
            } catch (Exception e) {
                SdtdConsole.Instance.Output($"Failed to run command.\n{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
