using Core;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Character
{
    public static class CharacterAssetLoader
    {
        private static string Normalize(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return characterName;
            return characterName.EndsWith("Root")
                ? characterName.Substring(0, characterName.Length - 4)
                : characterName;
        }

        public static void LoadCharacterAssets(PlayerController controller, string characterName)
        {
            if (controller == null || string.IsNullOrEmpty(characterName))
                return;

            string name = Normalize(characterName);

            // 1. 属性配置 SO（角色专属 → 默认 fallback）
            var attrConfig = AssetLoader.TryLoadAsset<PlayerAttributeConfigSO>($"Lobby_Config_PlayerConfig_{name}Attr");
            if (attrConfig == null)
                attrConfig = AssetLoader.TryLoadAsset<PlayerAttributeConfigSO>("Lobby_Config_PlayerConfig_PlayerAttributeConfig");
            if (attrConfig != null)
                controller.SetAttributeConfig(attrConfig);

            // 2. 模型 — 仅当预制体上没有 SkinnedMeshRenderer 时才从 Addressables 加载
            //    旧角色预制体已自带完整的 Armature + SkinnedMeshRenderer，无需加载
            bool hasModel = controller.GetComponentInChildren<SkinnedMeshRenderer>() != null;
            if (!hasModel)
            {
                var modelPrefab = AssetLoader.TryLoadAsset<GameObject>($"Lobby_Characters_{name}_Model");
                if (modelPrefab != null)
                {
                    var model = Object.Instantiate(modelPrefab, controller.transform);
                    model.name = name + "_Model";
                }
            }

            // 3. 动画控制器 — 仅当 Animator 没有控制器时才从 Addressables 加载
            var animator = controller.GetAnimator();
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var animController = AssetLoader.TryLoadAsset<RuntimeAnimatorController>($"Lobby_Characters_{name}_Anim");
                if (animController != null)
                    animator.runtimeAnimatorController = animController;
            }
        }

        public static void PreloadCharacterAssets(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return;
            string name = Normalize(characterName);

            AssetLoader.TryLoadAsset<PlayerAttributeConfigSO>($"Lobby_Config_PlayerConfig_{name}Attr");
            AssetLoader.TryLoadAsset<GameObject>($"Lobby_Characters_{name}_Model");
            AssetLoader.TryLoadAsset<RuntimeAnimatorController>($"Lobby_Characters_{name}_Anim");
        }
    }
}
