namespace LagShield {
    public class ModApi : IModApi {
        public void InitMod(Mod _modInstance) {
            ModEvents.GameStartDone.RegisterHandler(Service.Load);
            ModEvents.SavePlayerData.RegisterHandler(Service.CheckLag);
        }
    }
}
