namespace LagShield {
    internal class Violation {
        public string SteamId { get; set; }
        public string Name { get; set; }
        public int LagFailures { get; set; }
        public int TimesKicked { get; set; }

        public Violation(string steamId, string name) {
            SteamId = steamId;
            Name = name;
            LagFailures = 0;
            TimesKicked = 0;
        }

        public override string ToString() {
            return $"Name: {Name}, LagFailures: {LagFailures}, TimesKicked: {TimesKicked}, SteamId: {SteamId}";
        }
    }
}
