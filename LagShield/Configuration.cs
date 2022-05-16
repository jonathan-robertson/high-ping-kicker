namespace LagShield {
    public class Configuration {
        public int MaxPingAllowed { get; set; } = 200;
        public int FailureThresholdBeforeKick { get; set; } = 2;
        public int AllowedKicksBeforeBan { get; set; } = 2;
        public int HoursBannedAfterKickWarnings { get; set; } = 24;

        public override string ToString() {
            return $@"- MaxPingAllowed: {MaxPingAllowed}
- FailureThresholdBeforeKick: {FailureThresholdBeforeKick}
- AllowedKicksBeforeBan: {AllowedKicksBeforeBan}
- HoursBannedAfterKickWarnings: {HoursBannedAfterKickWarnings}";
        }
    }
}
