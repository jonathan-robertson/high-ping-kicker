using Platform.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HighPingKicker {
    internal class Service {
        private static readonly ModLog log = new ModLog(typeof(Service));
        public static string Path { get; private set; } = System.IO.Path.Combine(GameIO.GetSaveGameDir(), "high-ping-kicker.json");

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

            // good ping allows violations to recover
            if (ping <= Config.MaxPingAllowed && Violations.TryGetValue(key, out var recoveringViolation)) {
                recoveringViolation.PingFailures--;
                if (recoveringViolation.PingFailures == 0) {
                    Violations.Remove(key);
                }
                log.Info($"Successful ping for {ClientToId(clientInfo)}({clientInfo.playerName}): {ping}ms <= {Config.MaxPingAllowed}ms. Ping failure budget recovering: {recoveringViolation.PingFailures}/{Config.FailureThresholdBeforeKick}.");
                return;
            }

            // get or create violation object
            if (!Violations.TryGetValue(key, out var violation)) {
                violation = new Violation(key, clientInfo.playerName);
                Violations.Add(key, violation);
            }

            // react to ping failure
            violation.PingFailures++;
            log.Info($"Ping Failure for {ClientToId(clientInfo)}({clientInfo.playerName}): {ping}ms > {Config.MaxPingAllowed}ms. Ping failure budget impacted: {violation.PingFailures}/{Config.FailureThresholdBeforeKick}.");
            if (violation.PingFailures > Config.FailureThresholdBeforeKick) {
                violation.TimesKicked++;
                if (violation.TimesKicked > Config.AllowedKicksBeforeBan) {
                    var banExpiration = DateTime.Now.AddHours(Config.HoursBannedAfterKickWarnings);
                    KickAndBan(clientInfo, banExpiration);
                    BroadcastMessage(clientInfo, $"{clientInfo.playerName} was automatically banned for {Config.HoursBannedAfterKickWarnings}hrs after being auto-kicked multiple times for excessive latency above {Config.MaxPingAllowed}ms.");
                    violation.PingFailures = 0;
                    violation.TimesKicked = 0;
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} banned. Ping: {ping}");

                    // Don't ban family accounts... for now
                    // BanFamilyAccount(clientInfo, banExpiration, reason);
                } else {
                    var reason = $"Your connection to us exceeded the latency limit of {Config.MaxPingAllowed}ms multiple times, so you were automatically kicked from the server. Consider checking your router/computer/network to ensure your connection to us is functioning properly before attempting to reconnect. You can reconnect when ready, but please be aware that being kicked multiple times for this issue may result in a ban.";
                    Kick(clientInfo, reason);
                    BroadcastMessage(clientInfo, $"{clientInfo.playerName} was automatically kicked exceeded the latency limit of {Config.MaxPingAllowed}ms multiple times.");
                    violation.PingFailures = 0;
                    log.Warn($"{clientInfo.playerName}/{ClientToId(clientInfo)} kicked. Ping: {ping}");
                }
            }
        }

        private void Kick(ClientInfo clientInfo, string reason, GameUtils.EKickReason kickReason = GameUtils.EKickReason.ModDecision) {
            log.Info($"Kicked {ClientToId(clientInfo)}({clientInfo.playerName})");
            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(kickReason, 0, default, reason));
        }

        private void KickAndBan(ClientInfo clientInfo, DateTime banExpiration) {
            log.Info($"Banned {ClientToId(clientInfo)}({clientInfo.playerName})");
            Kick(clientInfo, "You were banned for excessive Network Latency and can try connecting again after the indicated time shown here. If you do not believe there is an ongoing problem with your network, you may simply be located too far away from this server. In that case, we would recommend trying a different server.", GameUtils.EKickReason.Banned);
            GameManager.Instance.adminTools.AddBan(clientInfo.playerName, clientInfo.PlatformId, banExpiration, $"Banned for {Config.HoursBannedAfterKickWarnings}hrs after auto-kicked multiple times for latency above {Config.MaxPingAllowed}ms.", true);
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
