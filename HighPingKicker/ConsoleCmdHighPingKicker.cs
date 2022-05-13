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
            return $@"Usage:
  1. {commands[0]}
  2. {commands[0]} set <Option> <Value>
  3. {commands[0]} reset
Description Overview
1. View current configuration
2. Update a key/value pair in the configuration
3. Delete the configuration file and create a new one with default values";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
            switch (_params.Count) {
                case 0:
                    SdtdConsole.Instance.Output(Service.Instance.SerializedConfig());
                    break;
                case 1:
                    if ("reset".EqualsCaseInsensitive(_params[1])) {
                        Service.Reset(SdtdConsole.Instance);
                    }
                    break;
                case 3:
                    if ("set".EqualsCaseInsensitive(_params[1]) && int.TryParse(_params[3], out var value)) {
                        try {
                            if (Service.Instance.Set(_params[2], value)) {
                                SdtdConsole.Instance.Output($"Successfully updated {_params[2]} -> {value}\nUpdated config file at {Service.Path}.");
                            } else {
                                SdtdConsole.Instance.Output($"Invalid option provided: {_params[2]}\nUse command '{commands[0]}' to view as list of options you can change.");
                            }
                        } catch (Exception e) {
                            SdtdConsole.Instance.Output($"Failed to update file at {Service.Path}.\n{e.Message}\n{e.StackTrace}");
                        }
                    }
                    break;
            }
        }
    }
}
