using System;

namespace Core.SharedModel
{
    [Serializable]
    public struct RunePower
    {
        public int value;      // e.g. 30
        public string label;   // e.g. "%基础伤害"
    }
}
