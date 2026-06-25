using System.Collections;
using Domain.Event;
using Domain.Manager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Infrastructure.Rendering
{
    public class GlobalVolumeManager : MonoBehaviour
    {
        private Volume volume;
        private Bloom bloom;
        private float originalIntensity;

        void Awake()
        {
            volume = GetComponent<Volume>();

            if (volume == null)
            {
                Debug.LogError("GlobalVolumeManager: Volume component not found on the GameObject.");
                return;
            }

            if (volume.profile.TryGet<Bloom>(out bloom))
            {
                originalIntensity = bloom.intensity.value;
            }
            else
            {
                Debug.LogError("GlobalVolumeManager: Bloom not found in Volume profile.");
            }
        }

        void OnEnable()
        {
            if (EventChannelLocator.MainContainer != null)
                EventChannelLocator.MainContainer.bloomChannel.RegisterListener(OnBloomIntensityChanged);
        }

        void OnDisable()
        {
            if (EventChannelLocator.MainContainer != null)
                EventChannelLocator.MainContainer.bloomChannel.UnregisterListener(OnBloomIntensityChanged);
        }

        private void OnBloomIntensityChanged(float intensity)
        {
            SetBloomIntensity(intensity);
        }

        // 全局控制Bloom强度
        public void SetBloomIntensity(float intensity)
        {
            if (bloom == null) return;
            StartCoroutine(SmoothSetBloomIntensity(intensity, 0.5f));
        }

        // 立即设置Bloom强度
        public void SetBloomIntensityImmediate(float intensity)
        {
            if (bloom != null)
            {
                bloom.intensity.value = intensity;
            }
        }

        // 重置到原始强度
        public void ResetBloomIntensity()
        {
            SetBloomIntensityImmediate(originalIntensity);
        }

        // 协程：平滑调整Bloom强度
        public IEnumerator SmoothSetBloomIntensity(float targetIntensity, float duration)
        {
            if (bloom == null) yield break;

            float startTime = Time.time;
            float startIntensity = bloom.intensity.value;
            float t = 0f;

            while (t < 1f)
            {
                t = (Time.time - startTime) / duration;
                bloom.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
                yield return null;
            }

            bloom.intensity.value = targetIntensity;
        }
    }
}