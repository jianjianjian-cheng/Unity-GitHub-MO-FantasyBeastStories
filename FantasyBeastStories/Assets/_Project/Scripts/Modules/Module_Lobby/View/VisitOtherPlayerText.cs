using System;
using System.Collections;
using Controllers.Player;
using Core.Contracts;
using Core.Network;
using DG.Tweening;
using UI.Framework.Base;
using UnityEngine;

namespace UI.Framework
{
    /// <summary>
    /// 队友死亡提示 Widget
    ///
    /// 职责：
    /// - 监听 PlayerManager.OnPlayerDeath 事件
    /// - 当队友（非本地玩家）死亡时，淡入显示提示文本
    ///
    /// 挂在 NGC1999VoidCanvas 下的 VisitOtherPlayerText 上
    /// </summary>
    public class VisitOtherPlayerText : UIWidget
    {
        [Header("VisitOtherPlayerText 设置")]
        [Tooltip("淡入动画时长（秒）")]
        [SerializeField] private float fadeInDuration = 0.5f;

        private CanvasGroup _canvasGroup;

        protected override void AutoBindComponents()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetVisible(false);
        }

        protected override void SubscribeEvents()
        {
            PlayerManager.OnPlayerDeath += OnPlayerDeath;
        }

        protected override void UnsubscribeEvents()
        {
            PlayerManager.OnPlayerDeath -= OnPlayerDeath;
        }

        private void OnPlayerDeath(string deadActorNumber)
        {
            // 仅当死亡的是队友（非本地玩家）时显示
            int localActorNumber = NetworkServiceLocator.PlayerService.GetLocalActorNumber();
            if (deadActorNumber == localActorNumber.ToString())
                return;

            Show();
        }

        /// <summary>淡入显示提示文本</summary>
        public void Show()
        {
            SetVisible(true);
            _canvasGroup.alpha = 0f;

            _canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }

        /// <summary>隐藏提示文本</summary>
        public void Hide()
        {
            _canvasGroup.DOFade(0f, fadeInDuration).SetEase(Ease.InQuad)
                .OnComplete(() => SetVisible(false));
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _canvasGroup?.DOKill();
        }
    }
}
