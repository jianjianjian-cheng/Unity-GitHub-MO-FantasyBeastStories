namespace Domain.Event
{
    public static class EventNames
    {
        public const string DamageReceived = "DamageReceived";
        public const string UpdateAttributePlayer = "UpdateAttributePlayer";
        public const string RuneInfo = "RuneInfo";
        public const string ChangeCanRotate = "ChangeCanRotate";

        public const string PlayerAttribute_Main = "MainPlayer";
        public const string PlayerAttribute_Current = "CurrentPlayer";
        public const string HPChanged = "HPChanged";
        public const string DamageReceiverPlayer = "DamageReceiverPlayer";

        public const string OnReceiveCard_WizardBoy = "OnReceiveCard_WizardBoy";
        public const string OnGetMaxAttackCount_WizardBoy = "OnGetMaxAttackCount_WizardBoy";

        public const string TimeEventTriggered = "TimeEventTriggered";
        public const string GameTimeUpdated = "GameTimeUpdated";
        public const string GameTimeFinished = "GameTimeFinished";
        public const string TimeSyncReceived = "TimeSyncReceived";
        public const string TimeStarted = "TimeStarted";
        public const string TimePaused = "TimePaused";
        public const string TimeResumed = "TimeResumed";
        public const string TimeReset = "TimeReset";

        public const string TimeChangeEnemyAttribute = "TimeChangeEnemyAttribute";
    }
}
