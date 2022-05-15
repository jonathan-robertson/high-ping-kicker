namespace HighPingKicker {
    internal class Violation {
        public string SteamId { get; set; }
        public string Name { get; set; }
        public int PingFailures { get; set; }
        public int TimesKicked { get; set; }

        public Violation(string steamId, string name) {
            SteamId = steamId;
            Name = name;
            PingFailures = 0;
            TimesKicked = 0;
        }

        public override string ToString() {
            return $"Name: {Name}, PingFailures: {PingFailures}, TimesKicked: {TimesKicked}, SteamId: {SteamId}";
        }
    }
}
