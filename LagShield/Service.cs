using Platform.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LagShield {
    internal class Service {
        private static readonly ModLog log = new ModLog(typeof(Service));
        public static string Path { get; private set; } = System.IO.Path.Combine(GameIO.GetSaveGameDir(), "lag-shield.json");

        public Configuration Config { get; private set; }
        public Dictionary<string, Violation> Violations { get; private set; } = new Dictionary<string, Violation>();
        public static Service Instance {
            get {
                if (instance == null) {
                    Load();
                }
                return instance;
            }
        }
        private static Service instance;

        public static void CheckLag(ClientInfo clientInfo, PlayerDataFile _) {
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

            // good ping allows violations to recover
            if (ping <= Config.MaxPingAllowed) {
                if (Violations.TryGetValue(key, out var recoveringViolation)) {
                    recoveringViolation.LagFailures--;
                    if (recoveringViolation.LagFailures <= 0) {
                        Violations.Remove(key);
                    }
                    log.Info($"Successful ping for {ClientToId(clientInfo)}({clientInfo.playerName}): {ping}ms <= {Config.MaxPingAllowed}ms. Ping failure budget recovering: {recoveringViolation.LagFailures}/{Config.FailureThresholdBeforeKick}.");
                }
                return;
            }

            // get or create violation object
            if (!Violations.TryGetValue(key, out var violation)) {
                violation = new Violation(key, clientInfo.playerName);
                Violations.Add(key, violation);
            }

            // react to ping failure
            violation.LagFailures++;
            log.Info($"Ping Failure for {ClientToId(clientInfo)}({clientInfo.playerName}): {ping}ms > {Config.MaxPingAllowed}ms. Ping failure budget impacted: {violation.LagFailures}/{Config.FailureThresholdBeforeKick}.");
            if (violation.LagFailures > Config.FailureThresholdBeforeKick) {
                violation.TimesKicked++;
                if (violation.TimesKicked > Config.AllowedKicksBeforeBan) {
                    var banExpiration = DateTime.Now.AddHours(Config.HoursBannedAfterKickWarnings);
                    KickAndBan(clientInfo, banExpiration);
                    BroadcastMessage(clientInfo, $"{clientInfo.playerName} was automatically banned for {Config.HoursBannedAfterKickWarnings}hrs after being auto-kicked multiple times for excessive latency above {Config.MaxPingAllowed}ms.");
                    violation.LagFailures = 0;
                    violation.TimesKicked = 0;
                    Violations.Remove(key);
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} banned. Ping: {ping}");

                    // Don't ban family accounts... for now
                    // BanFamilyAccount(clientInfo, banExpiration, reason);
                } else {
                    Kick(clientInfo, "Your connection to us exceeded our allowed latency limit, so you were automatically kicked. Try checking your router/computer/network to ensure your connection is stable before attempting to rejoin. Repeated kicking may result in a ban.");
                    BroadcastMessage(clientInfo, $"{clientInfo.playerName} was automatically kicked exceeded the latency limit of {Config.MaxPingAllowed}ms multiple times.");
                    violation.LagFailures = 0;
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} kicked. Ping: {ping}");
                }
            }
        }

        private void Kick(ClientInfo clientInfo, string reason) {
            log.Info($"Kicked {ClientToId(clientInfo)}({clientInfo.playerName})");
            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, reason));
        }

        private void KickAndBan(ClientInfo clientInfo, DateTime banExpiration) {
            log.Info($"Banned {ClientToId(clientInfo)}({clientInfo.playerName})");
            GameManager.Instance.adminTools.AddBan(clientInfo.playerName, clientInfo.PlatformId, banExpiration, $"High network latency when connecting to our server triggered in a temporary ban for {Config.HoursBannedAfterKickWarnings}hrs. Try joining again once your connection improves or join a different server if needed.", true);
            Kick(clientInfo, "You have been banned for 24 hrs due to high ping/latency. Once the ban expires, you are free to reconnect, but your expeirence might not improve if you're located far enough away from our server.");
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

        private string ClientToId(ClientInfo clientInfo) {
            return clientInfo.PlatformId.ReadablePlatformUserIdentifier;
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
