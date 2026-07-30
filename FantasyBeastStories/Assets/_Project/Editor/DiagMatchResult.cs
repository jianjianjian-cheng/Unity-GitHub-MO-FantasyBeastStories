using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Core;
using Controllers.Battle;
using UI.Framework.Manager;

public static class DiagMatchResult
{
    [MenuItem("Tools/Diagnose MatchResultPanel")]
    public static void Run()
    {
        Debug.Log("===== MatchResultPanel 诊断开始 =====");

        // 1. EventChannelLocator
        var container = EventChannelLocator.MainContainer;
        if (container == null)
        {
            Debug.LogError("[Diag] EventChannelLocator.MainContainer is NULL!");
            return;
        }
        Debug.Log("[Diag] EventChannelLocator.MainContainer: OK");

        // 2. matchStatsUpdateChannel
        var channel = container.matchStatsUpdateChannel;
        Debug.Log($"[Diag] matchStatsUpdateChannel: {(channel != null ? "OK" : "NULL")}");

        if (channel != null)
        {
            var channelType = channel.GetType();
            FieldInfo listenerField = null;
            for (var t = channelType; t != null && t != typeof(object); t = t.BaseType)
            {
                listenerField = t.GetField("listeners", BindingFlags.NonPublic | BindingFlags.Instance);
                if (listenerField != null) break;
            }
            if (listenerField != null)
            {
                var listeners = listenerField.GetValue(channel) as System.Collections.ICollection;
                Debug.Log($"[Diag] matchStatsUpdateChannel listeners count: {(listeners != null ? listeners.Count : "null")}");
            }
            else
            {
                Debug.Log("[Diag] Could not find 'listeners' field, dumping all fields:");
                for (var t = channelType; t != null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        var val = f.GetValue(channel);
                        if (val is System.Collections.ICollection col)
                            Debug.Log($"[Diag]   {t.Name}.{f.Name} = {col.Count} items ({f.FieldType.Name})");
                    }
                }
            }
        }

        // 3. MatchStatisticsManager
        var matchStats = ServiceLocator.Get<MatchStatisticsManager>();
        if (matchStats == null)
        {
            Debug.LogError("[Diag] MatchStatisticsManager NOT registered in ServiceLocator!");
        }
        else
        {
            Debug.Log($"[Diag] MatchStatisticsManager: OK");
            Debug.Log($"[Diag]   HasPendingMatchResult: {matchStats.HasPendingMatchResult}");
            Debug.Log($"[Diag]   TotalKillsInMatch: {matchStats.GetTotalKillsInMatch()}");
            Debug.Log($"[Diag]   Model.TotalExpInMatch: {matchStats.Model.TotalExpInMatch}");
        }

        // 4. UIManager
        var uiManager = UIManager.Instance;
        Debug.Log($"[Diag] UIManager.Instance: {(uiManager != null ? "OK" : "NULL")}");

        // 5. Loading — skip type lookup, just note it
        Debug.Log("[Diag] Loading: (check via ServiceLocator at runtime)");

        // 6. MatchResultPanel in current scene
        var panel = Object.FindObjectsOfType<MonoBehaviour>(true)
            .FirstOrDefault(mb => mb.GetType().Name == "MatchResultPanel");
        Debug.Log($"[Diag] MatchResultPanel in active scene: {(panel != null ? $"FOUND on '{panel.gameObject.name}' active={panel.gameObject.activeInHierarchy}" : "NOT FOUND")}");

        // 7. Check lobby scene for MatchResultPanel
        var lobbyPath = "Assets/Scenes/WaitLobby.unity";
        var lobbyGOs = AssetDatabase.LoadAssetAtPath<GameObject>(lobbyPath);
        Debug.Log($"[Diag] Lobby scene root asset: {(lobbyGOs != null ? "OK" : "NULL (scenes don't load as GameObjects)")}");

        // 8. Simulate: check if FinalizeMatch would produce data
        if (matchStats != null)
        {
            int kills = matchStats.GetTotalKillsInMatch();
            int damage = matchStats.Model.TotalDamageInMatch;
            int exp = matchStats.Model.TotalExpInMatch;
            bool hasData = kills > 0 || damage > 0 || exp > 0;
            Debug.Log($"[Diag] FinalizeMatch would have hasActualData={hasData} (kills={kills}, damage={damage}, exp={exp})");
            if (!hasData)
                Debug.LogWarning("[Diag] ⚠ FinalizeMatch will NOT raise event because hasActualData=false — stats are all zero!");
        }

        Debug.Log("===== 诊断结束 =====");
    }
}
