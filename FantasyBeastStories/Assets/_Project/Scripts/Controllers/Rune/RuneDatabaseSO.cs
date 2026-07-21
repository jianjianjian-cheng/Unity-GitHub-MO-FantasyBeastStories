using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rune/Rune Database")]
public class RuneDatabaseSO : ScriptableObject
{
    public List<RuneDataSO> allRunes;

    public RuneDataSO GetRuneById(int id) => allRunes.Find(r => r.runeId == id);

    public bool HasRune(int id) => allRunes.Exists(r => r.runeId == id);
}