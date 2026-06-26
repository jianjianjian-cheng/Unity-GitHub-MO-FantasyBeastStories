namespace Domain.Event
{
    public struct HealthUpdateData
    {
        public string playerId;
        public float maxHp;
        public float currentHp;
        public bool isLocalPlayer;

        public static HealthUpdateData OtherPlayer(string playerId, float maxHp, float currentHp)
        {
            return new HealthUpdateData
            {
                playerId = playerId,
                maxHp = maxHp,
                currentHp = currentHp,
                isLocalPlayer = false
            };
        }
    }
}
