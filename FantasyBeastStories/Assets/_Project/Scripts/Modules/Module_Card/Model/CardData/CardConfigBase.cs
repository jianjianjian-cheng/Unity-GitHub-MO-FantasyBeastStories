using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Card
{
    public class CardConfigBase
    {
        public string Name;
        public string Content;
        public int Value;
        public string Quality;

        [SerializeReference]
        public List<ICardEffect> Effects;
    }
}
