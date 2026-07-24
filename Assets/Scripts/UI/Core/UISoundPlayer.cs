using UnityEngine;
using UnityEngine.UI;

namespace MCFight
{
    /// <summary>
    /// UI 音效播放器：统一管理按钮点击、切换、购买等音效。
    /// 挂载在 Canvas 上，各按钮通过 UIButtonStyled 自动调用。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UISoundPlayer : MonoBehaviour
    {
        private static UISoundPlayer _instance;
        private AudioSource _audio;

        [Header("音效引用")]
        public AudioClip ClickSound;
        public AudioClip SwitchSound;
        public AudioClip TapSound;
        public AudioClip CancelSound;

        public static UISoundPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var canvas = FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        _instance = canvas.GetComponent<UISoundPlayer>();
                        if (_instance == null)
                            _instance = canvas.gameObject.AddComponent<UISoundPlayer>();
                    }
                }
                return _instance;
            }
        }

        void Awake()
        {
            _instance = this;
            _audio = GetComponent<AudioSource>();
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.volume = 0.6f;
            }
        }

        public void PlayClick()
        {
            if (ClickSound != null) _audio.PlayOneShot(ClickSound);
        }

        public void PlaySwitch()
        {
            if (SwitchSound != null) _audio.PlayOneShot(SwitchSound);
        }

        public void PlayConfirm()
        {
            if (TapSound != null) _audio.PlayOneShot(TapSound);
        }

        public void PlayCancel()
        {
            if (CancelSound != null) _audio.PlayOneShot(CancelSound);
        }
    }
}
