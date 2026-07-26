using System.Collections;
using UnityEngine;

namespace UI.Framework.Animation
{
    public interface IUIAnimation
    {
        IEnumerator PlayCoroutine(GameObject target);
        void Stop();
        bool IsPlaying { get; }
    }
}
