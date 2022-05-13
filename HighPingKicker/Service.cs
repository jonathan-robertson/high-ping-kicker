using Platform.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HighPingKicker {
    internal class Service {
        private static readonly ModLog log = new ModLog(typeof(Service));
        public static string Path { get; private set; } = System.IO.Path.Combine(GameIO.GetSaveGameDir(), "high-ping-kicker.json");

        private readonly Configuration Config;
        private readonly Dictionary<string, int> PingCounters = new Dictionary<string, int>();
        private readonly Dictionary<string, int> KickCounters = new Dictionary<string, int>();

        public static Service Instance {
            get {
                if (instance == null) {
                    Load();
                }
                return instance;
            }
        }
        private static Service instance;

        public static void CheckPing(ClientInfo clientInfo, PlayerDataFile _) {
            try {
                if (Instance != null) {
                    Instance.CheckPing(clientInfo);
                }
            } catch (Exception) {
                // ignore
            }
        }

        private void CheckPing(ClientInfo clientInfo) {
            var ping = clientInfo.ping;
            var key = ClientToId(clientInfo);
            log.Debug("Ping Check: {ClientToId(clientInfo)}({clientInfo.playerName}): {ping}ms"); // TODO: remove
            if (ping <= Config.MaxPingAllowed) {
                Decrement(PingCounters, key);
                return;
            }

            if (IncrementThenGet(PingCounters, key) > Config.FailureThresholdBeforeKick) {
                if (IncrementThenGet(KickCounters, key) > Config.AllowedKicksBeforeBan) {
                    var banExpiration = DateTime.Now.AddHours(Config.HoursBannedAfterKickWarnings);
                    var reason = $"kicked multiple times for excessive latency above {Config.MaxPingAllowed}ms but the problem continues to persist";
                    Ban(clientInfo, banExpiration, reason);
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} was banned: {reason}");

                    // Don't ban family accounts... for now
                    // BanFamilyAccount(clientInfo, banExpiration, reason);
                } else {
                    var reason = $"latency exceeded the limit of {Config.MaxPingAllowed}ms multiple times, but can reconnect after checking router/network";
                    Kick(clientInfo, reason);
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} was kicked: {reason}");
                }
            }
        }

        private void Kick(ClientInfo clientInfo, string reason, GameUtils.EKickReason kickReason = GameUtils.EKickReason.ModDecision) {
            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(kickReason, 0, default, reason));
            BroadcastMessage(clientInfo, $"{clientInfo.playerName} was kicked: {reason}");
        }

        private void Ban(ClientInfo clientInfo, DateTime banExpiration, string reason) {
            Kick(clientInfo, reason, GameUtils.EKickReason.Banned);
            BroadcastMessage(clientInfo, $"{clientInfo.playerName} was banned for {Config.HoursBannedAfterKickWarnings}hrs: {reason}");
            GameManager.Instance.adminTools.AddBan(clientInfo.playerName, clientInfo.PlatformId, banExpiration, "Excessive Network Latency; Please check your router and network before connecting again or consider finding another server if this has happened multiple times.", true);
        }

        private void BanFamilyAccount(ClientInfo clientInfo, DateTime banExpiration, string reason) {
            if (clientInfo.PlatformId is UserIdentifierSteam userIdentifierSteam && !userIdentifierSteam.OwnerId.Equals(userIdentifierSteam)) {
                GameManager.Instance.adminTools.AddBan(clientInfo.playerName, userIdentifierSteam.OwnerId, banExpiration, reason, true);
            }
        }

        private void BroadcastMessage(ClientInfo clientInfo, string message) {
            GameManager.Instance.ChatMessageServer(clientInfo, EChatType.Global, -1, message, null, false, GameManager.Instance.World.Players.list
                .Select(p => p.entityId).ToList());
        }

        private int IncrementThenGet(Dictionary<string, int> counters, string key) {
            if (counters.ContainsKey(key)) {
                counters[key]++;
            } else {
                counters.Add(key, 1);
            }
            return counters[key];
        }

        private void Decrement(Dictionary<string, int> counters, string key) {
            if (counters.ContainsKey(key)) {
                counters[key]--;
                if (counters[key] == 0) {
                    counters.Remove(key);
                }
            }
        }

        private string ClientToId(ClientInfo clientInfo) {
            return clientInfo.PlatformId.ReadablePlatformUserIdentifier;
        }

        private void ResetViolationsCounter(ClientInfo clientInfo) {
            var key = ClientToId(clientInfo);
            if (PingCounters.ContainsKey(key)) {
                PingCounters.Remove(key);
            }
        }

        public static bool Load() {
            try {
                var config = JsonUtil.Deserialize<Configuration>(File.ReadAllText(Path));
                log.Info($"Successfully loaded config from {Path}.");
                instance = new Service(config);
                return true;
            } catch (FileNotFoundException) {
                log.Warn($"File not found at {Path}; creating a new one with default configs.");
                instance = new Service(null);
                return true;
            } catch (Exception e) {
                log.Error($"Could not load file at {Path}; use console command 'hpk reset' to reset this file to defaults if you're unable to access/edit it directly.", e);
                return false;
            }
        }
        public static void Reset(SdtdConsole console) {
            try {
                File.Delete(Path);
                instance = new Service(null);
                console.Output($"Successfully deleted file at {Path} and re-initialized the service with the default configuration.");
            } catch (Exception e) {
                var message = $"Failed to delete file at {Path}; check logs for more info.";
                log.Error(message, e);
                console.Output(message);
            }
        }

        private Service(Configuration config) {
            if (config != null) {
                Config = config;
            } else {
                Config = new Configuration();
            }
        }

        public bool Set(string v, int value) {
            if ("MaxPingAllowed".EqualsCaseInsensitive(v)) {
                Config.MaxPingAllowed = value;
            } else if ("FailureThresholdBeforeKick".EqualsCaseInsensitive(v)) {
                Config.FailureThresholdBeforeKick = value;
            } else if ("AllowedKicksBeforeBan".EqualsCaseInsensitive(v)) {
                Config.AllowedKicksBeforeBan = value;
            } else if ("HoursBannedAfterKickWarnings".EqualsCaseInsensitive(v)) {
                Config.HoursBannedAfterKickWarnings = value;
            } else {
                return false;
            }
            Save(); // may throw exception
            return true;
        }

        public string SerializedConfig() {
            return JsonUtil.Serialize(Config);
        }

        public void Save() {
            try {
                File.WriteAllText(Path, SerializedConfig());
                log.Info($"Successfully saved config to {Path}.");
            } catch (Exception e) {
                log.Error($"Unable to save to {Path}.", e);
                throw e;
            }
        }
    }
}
