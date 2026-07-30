using System.Collections.Generic;
using System.IO;
using Controllers.Card;
using Core.SharedModel;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// 卡牌迁移脚本
    /// 将旧的 CardDatabasePublic + CardDatabaseEX 中的数组元素
    /// 迁移为独立的 CardConfigSO .asset 文件，并创建统一的 CardDatabaseSO
    ///
    /// 使用方式：菜单栏 Tools → Card Migration → Migrate Cards
    /// </summary>
    public static class CardMigrationTool
    {
        private const string OUTPUT_FOLDER = "Assets/_Project/Addressables/CardData/Cards";
        private const string DATABASE_PATH = "Assets/_Project/Addressables/CardData/CardDatabase.asset";

        [MenuItem("Tools/Card Migration/Migrate Cards")]
        public static void MigrateCards()
        {
            // 1. 查找旧的数据库
            var publicDbGuids = AssetDatabase.FindAssets("t:CardDatabasePublic");
            var exDbGuids = AssetDatabase.FindAssets("t:CardDatabaseEX");

            if (publicDbGuids.Length == 0 && exDbGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("迁移", "未找到 CardDatabasePublic 或 CardDatabaseEX 资产，请确认路径正确。", "OK");
                return;
            }

            // 确认执行
            if (!EditorUtility.DisplayDialog("确认迁移",
                "将把旧数据库中的卡牌迁移为独立 CardConfigSO 资产。\n\n" +
                "• 旧数据库不会被删除\n" +
                "• 新卡牌将创建在 Assets/_Project/Addressables/CardData/Cards/\n" +
                "• 新数据库创建在 Assets/_Project/Addressables/CardData/CardDatabase.asset\n\n" +
                "确认执行？", "执行", "取消"))
            {
                return;
            }

            // 2. 创建输出目录
            if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            {
                Directory.CreateDirectory(OUTPUT_FOLDER);
                AssetDatabase.Refresh();
            }

            var allNewCards = new List<CardConfigSO>();
            int cardIndex = 0;

            // 3. 迁移公用卡
            foreach (var guid in publicDbGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<CardDatabasePublic>(path);
                if (db == null) continue;

                Debug.Log($"[CardMigration] 正在迁移 CardDatabasePublic: {path}");

                cardIndex += MigratePublicCards(db.cardsPublicNormal, CardQuality.Normal, allNewCards, ref cardIndex);
                cardIndex += MigratePublicCards(db.cardsPublicEpic, CardQuality.Epic, allNewCards, ref cardIndex);
                cardIndex += MigratePublicCards(db.cardsPublicLegend, CardQuality.Legend, allNewCards, ref cardIndex);
            }

            // 4. 迁移专属卡
            foreach (var guid in exDbGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<CardDatabaseEX>(path);
                if (db == null) continue;

                Debug.Log($"[CardMigration] 正在迁移 CardDatabaseEX: {path}");

                cardIndex += MigrateExclusiveCards(db.cardsEX_WizardBoy, CharacterCardType.WizardBoy, allNewCards, ref cardIndex);
                cardIndex += MigrateExclusiveCards(db.cardsEX_BingNv, CharacterCardType.BingNv, allNewCards, ref cardIndex);
            }

            // 5. 创建 CardDatabaseSO
            var database = ScriptableObject.CreateInstance<CardDatabaseSO>();
            database.allCards = allNewCards;
            AssetDatabase.CreateAsset(database, DATABASE_PATH);
            EditorUtility.SetDirty(database);

            // 6. 保存
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("迁移完成",
                $"共迁移 {allNewCards.Count} 张卡牌\n" +
                $"新数据库: {DATABASE_PATH}\n" +
                $"卡牌目录: {OUTPUT_FOLDER}/", "OK");

            Debug.Log($"[CardMigration] 迁移完成！共 {allNewCards.Count} 张卡牌");
        }

        /// <summary>
        /// 迁移公用卡数组
        /// </summary>
        private static int MigratePublicCards<T>(T[] cards, CardQuality quality,
            List<CardConfigSO> output, ref int index) where T : CardConfigBase
        {
            if (cards == null) return 0;

            int count = 0;
            foreach (var card in cards)
            {
                if (card == null) continue;

                var newCard = CreateCardSO(card, quality, CardScope.Public, null, ref index);
                if (newCard != null)
                {
                    output.Add(newCard);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 迁移专属卡数组
        /// </summary>
        private static int MigrateExclusiveCards(CardConfigEX[] cards, string characterType,
            List<CardConfigSO> output, ref int index)
        {
            if (cards == null) return 0;

            int count = 0;
            foreach (var card in cards)
            {
                if (card == null) continue;

                // 专属卡的品质：从 Quality 字段解析，默认 Normal
                var quality = ParseQuality(card.Quality);

                var newCard = CreateCardSO(card, quality, CardScope.Exclusive, characterType, ref index);
                if (newCard != null)
                {
                    newCard.stackable = card.Stackable;
                    output.Add(newCard);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 创建单个 CardConfigSO 资产
        /// </summary>
        private static CardConfigSO CreateCardSO(CardConfigBase oldCard, CardQuality quality,
            CardScope scope, string characterType, ref int index)
        {
            if (oldCard == null) return null;

            index++;
            string safeName = string.IsNullOrEmpty(oldCard.Name) ? $"Card_{index}" : SanitizeFileName(oldCard.Name);
            string assetPath = $"{OUTPUT_FOLDER}/{safeName}.asset";

            // 避免文件名冲突
            if (File.Exists(assetPath))
            {
                assetPath = $"{OUTPUT_FOLDER}/{safeName}_{index}.asset";
            }

            var newCard = ScriptableObject.CreateInstance<CardConfigSO>();
            newCard.cardId = safeName;
            newCard.cardName = oldCard.Name;
            newCard.description = oldCard.Content;
            newCard.value = oldCard.Value;
            newCard.quality = quality;
            newCard.scope = scope;
            newCard.characterType = characterType ?? "";
            newCard.stackable = false;
            newCard.Effects = oldCard.Effects != null
                ? new List<ICardEffect>(oldCard.Effects)
                : new List<ICardEffect>();

            AssetDatabase.CreateAsset(newCard, assetPath);
            EditorUtility.SetDirty(newCard);

            Debug.Log($"[CardMigration]   创建: {assetPath} (quality={quality}, scope={scope}, effects={newCard.Effects.Count})");

            return newCard;
        }

        /// <summary>
        /// 解析品质字符串为枚举
        /// </summary>
        private static CardQuality ParseQuality(string qualityStr)
        {
            if (string.IsNullOrEmpty(qualityStr))
                return CardQuality.Normal;

            switch (qualityStr)
            {
                case "普通": return CardQuality.Normal;
                case "史诗": return CardQuality.Epic;
                case "传说": return CardQuality.Legend;
                default: return CardQuality.Normal;
            }
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var result = name;
            foreach (var c in invalidChars)
                result = result.Replace(c, '_');
            return result.Trim();
        }
    }
}
