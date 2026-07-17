// Assets/Scripts/Domain/Rune/RuneDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rune/Rune Database")]
public class RuneDatabaseSO : ScriptableObject
{
    public List<RuneDataSO> allRunes;

    public RuneDataSO GetRuneById(int id) => allRunes.Find(r => r.runeId == id);
}