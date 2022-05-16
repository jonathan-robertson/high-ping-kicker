namespace LagShield {
    internal class JsonUtil {
        public static string Serialize<T>(T data) {
            return SimpleJson2.SimpleJson2.SerializeObject(data);
        }

        public static T Deserialize<T>(string json) {
            return (T)SimpleJson2.SimpleJson2.DeserializeObject(json, typeof(T));
        }
    }
}
