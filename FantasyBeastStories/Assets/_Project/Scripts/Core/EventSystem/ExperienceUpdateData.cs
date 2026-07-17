namespace Core
{
    /// <summary>
    /// 经验值/等级更新数据，用于 Application → Presentation 层通信
    /// </summary>
    public class ExperienceUpdateData : EventArgsBase
    {
        /// <summary>当前经验值</summary>
        public int CurrentExperience { get; set; }

        /// <summary>升级所需经验值</summary>
        public int UpgradeExperience { get; set; }

        /// <summary>当前等级</summary>
        public int CurrentLevel { get; set; }

        /// <summary>Slider 进度值 (CurrentExperience / UpgradeExperience)</summary>
        public float SliderProgress => UpgradeExperience > 0
            ? (float)CurrentExperience / UpgradeExperience
            : 0f;

        public ExperienceUpdateData(int currentExperience, int upgradeExperience, int currentLevel)
        {
            CurrentExperience = currentExperience;
            UpgradeExperience = upgradeExperience;
            CurrentLevel = currentLevel;
        }
    }
}