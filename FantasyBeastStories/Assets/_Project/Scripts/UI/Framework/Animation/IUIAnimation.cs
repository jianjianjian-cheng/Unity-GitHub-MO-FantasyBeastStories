using System.Threading.Tasks;
using UnityEngine;

namespace UI.Framework.Animation
{
    public interface IUIAnimation
    {
        Task PlayAsync(GameObject target);
        void Play(GameObject target);
        void Stop();
        bool IsPlaying { get; }
    }
}