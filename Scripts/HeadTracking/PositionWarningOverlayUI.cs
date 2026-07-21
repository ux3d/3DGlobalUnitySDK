using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace G3D
{
    /// <summary>
    /// Runtime UI equivalent of the WinUI PositionWarningView.
    /// Builds a small overlay in code and shows one positional warning at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public class PositionWarningOverlayUI : MonoBehaviour
    {
        private enum WarnSide
        {
            None,
            Top,
            Bottom,
            Left,
            Right,
            Close,
            Far
        }

        [Tooltip(
            "Polls the SDK each frame. Disable this if another system calls UpdatePositionHint()."
        )]
        public bool pollFromSdk = true;

        [Tooltip("Canvas sorting order for the overlay.")]
        public int sortingOrder = 900;

        [Tooltip("How long one pulse takes. Hide is deferred until this cycle ends.")]
        public float pulseCycleSeconds = 0.7f;

        [Tooltip("Main warning text font size.")]
        public int warningFontSize = 52;

        private Canvas _canvas;
        private GameObject _root;
        private Image _overlayFade;

        private RectTransform _topIndicator;
        private RectTransform _bottomIndicator;
        private RectTransform _leftIndicator;
        private RectTransform _rightIndicator;
        private RectTransform _centerIndicator;

        private Text _topText;
        private Text _bottomText;
        private Text _leftText;
        private Text _rightText;
        private Text _centerText;

        private VideoPlayer _moveRightVideoPlayer;
        private RawImage _moveRightVideoImage;
        private RenderTexture _moveRightRenderTexture;
        private AspectRatioFitter _moveRightAspectFitter;
        private bool _hasMoveRightVideo;
        private bool _moveRightVideoShouldPlay;

        private WarnSide _currentSide = WarnSide.None;
        private float _sideShownAt = -1f;
        private float _hideAt = -1f;

        private void Awake()
        {
            BuildUi();
            SetupMoveRightVideo();
            HideAllNow();
        }

        private void OnDestroy()
        {
            if (_moveRightVideoPlayer != null)
            {
                _moveRightVideoPlayer.Stop();
                _moveRightVideoPlayer.targetTexture = null;
                _moveRightVideoPlayer.prepareCompleted -= OnMoveRightVideoPrepared;
                _moveRightVideoPlayer.errorReceived -= OnMoveRightVideoError;
                _moveRightVideoPlayer.loopPointReached -= OnMoveRightVideoLoopPointReached;
            }

            if (_moveRightRenderTexture != null)
            {
                _moveRightRenderTexture.Release();
                Destroy(_moveRightRenderTexture);
                _moveRightRenderTexture = null;
            }
        }

        private void OnEnable()
        {
            if (_root != null)
                _root.SetActive(true);
        }

        private void OnDisable()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Update()
        {
            if (pollFromSdk)
            {
                HeadTrackingSDK.HeadTrackingUserPosCodes code = HeadTrackingSDK.loaded()
                    ? HeadTrackingSDK.ht_get_user_guidance()
                    : HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_NO;
                UpdatePositionHint(code);
            }

            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                HideAllNow();
            }

            if (_currentSide != WarnSide.None)
            {
                AnimateActiveWarning();
            }
        }

        /// <summary>
        /// External entry point. Call this if another class already owns the hint value.
        /// </summary>
        public void UpdatePositionHint(HeadTrackingSDK.HeadTrackingUserPosCodes code)
        {
            WarnSide side = MapCodeToSide(code);
            ShowSide(side);
        }

        private WarnSide MapCodeToSide(HeadTrackingSDK.HeadTrackingUserPosCodes code)
        {
            switch (code)
            {
                // Top/bottom indicators are placed at the matching screen edge and
                // contain the corrective text (top => MOVE DOWN, bottom => MOVE UP).
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_UP:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_UP:
                    return WarnSide.Top;
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_DOWN:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_DOWN:
                    return WarnSide.Bottom;
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_LEFT:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_LEFT:
                    return WarnSide.Left;
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_RIGHT:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_RIGHT:
                    return WarnSide.Right;
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_FRONT:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_FRONT:
                    return WarnSide.Close;
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_BACK:
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_BACK:
                    return WarnSide.Far;
                default:
                    return WarnSide.None;
            }
        }

        private void ShowSide(WarnSide side)
        {
            if (side != WarnSide.None)
            {
                _hideAt = -1f;

                if (_currentSide == side)
                    return;

                _currentSide = side;
                _sideShownAt = Time.unscaledTime;

                SetActiveOnly(side);
                _root.SetActive(true);
                return;
            }

            if (_currentSide != WarnSide.None && _hideAt < 0f)
            {
                float elapsed = Mathf.Max(0f, Time.unscaledTime - _sideShownAt);
                float remaining = Mathf.Max(0f, pulseCycleSeconds - elapsed);
                _hideAt = Time.unscaledTime + remaining;
            }
        }

        private void AnimateActiveWarning()
        {
            if (_currentSide == WarnSide.Left && _hasMoveRightVideo)
            {
                _overlayFade.color = new Color(0f, 0f, 0f, 0.1f);
                return;
            }

            float phase = (Time.unscaledTime - _sideShownAt) / Mathf.Max(0.01f, pulseCycleSeconds);
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
            float scale = Mathf.Lerp(0.96f, 1.05f, pulse);
            float alpha = Mathf.Lerp(0.45f, 1f, pulse);

            RectTransform active = GetActiveIndicator();
            if (active != null)
            {
                active.localScale = new Vector3(scale, scale, 1f);
                Image img = active.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = alpha;
                    img.color = c;
                }
            }

            _overlayFade.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.06f, 0.14f, pulse));
        }

        private RectTransform GetActiveIndicator()
        {
            switch (_currentSide)
            {
                case WarnSide.Top:
                    return _topIndicator;
                case WarnSide.Bottom:
                    return _bottomIndicator;
                case WarnSide.Left:
                    return _leftIndicator;
                case WarnSide.Right:
                    return _rightIndicator;
                case WarnSide.Close:
                case WarnSide.Far:
                    return _centerIndicator;
                default:
                    return null;
            }
        }

        private void HideAllNow()
        {
            _currentSide = WarnSide.None;
            _hideAt = -1f;
            _sideShownAt = -1f;

            if (_root != null)
                _root.SetActive(false);

            if (_topIndicator != null)
                _topIndicator.gameObject.SetActive(false);
            if (_bottomIndicator != null)
                _bottomIndicator.gameObject.SetActive(false);
            if (_leftIndicator != null)
                _leftIndicator.gameObject.SetActive(false);
            if (_rightIndicator != null)
                _rightIndicator.gameObject.SetActive(false);
            if (_centerIndicator != null)
                _centerIndicator.gameObject.SetActive(false);

            if (_hasMoveRightVideo)
            {
                _moveRightVideoShouldPlay = false;
                _moveRightVideoPlayer.Stop();
                _moveRightVideoImage.enabled = false;

                Image leftImage = _leftIndicator.GetComponent<Image>();
                if (leftImage != null)
                    leftImage.enabled = true;

                if (_leftText != null)
                    _leftText.gameObject.SetActive(true);
            }
        }

        private void SetActiveOnly(WarnSide side)
        {
            _topIndicator.gameObject.SetActive(side == WarnSide.Top);
            _bottomIndicator.gameObject.SetActive(side == WarnSide.Bottom);
            _leftIndicator.gameObject.SetActive(side == WarnSide.Left);
            _rightIndicator.gameObject.SetActive(side == WarnSide.Right);
            _centerIndicator.gameObject.SetActive(side == WarnSide.Close || side == WarnSide.Far);

            _topIndicator.localScale = Vector3.one;
            _bottomIndicator.localScale = Vector3.one;
            _leftIndicator.localScale = Vector3.one;
            _rightIndicator.localScale = Vector3.one;
            _centerIndicator.localScale = Vector3.one;

            SetIndicatorAlpha(_topIndicator, 1f);
            SetIndicatorAlpha(_bottomIndicator, 1f);
            SetIndicatorAlpha(_leftIndicator, 1f);
            SetIndicatorAlpha(_rightIndicator, 1f);
            SetIndicatorAlpha(_centerIndicator, 1f);

            _topText.text = "MOVE DOWN";
            _bottomText.text = "MOVE UP";
            _leftText.text = "MOVE RIGHT";
            _rightText.text = "MOVE LEFT";
            _centerText.text = side == WarnSide.Close ? "MOVE AWAY" : "MOVE CLOSER";

            if (_hasMoveRightVideo)
            {
                Image leftImage = _leftIndicator.GetComponent<Image>();

                if (side == WarnSide.Left)
                {
                    _moveRightVideoImage.enabled = true;
                    _leftText.gameObject.SetActive(false);

                    if (leftImage != null)
                        leftImage.enabled = false;

                    _moveRightVideoShouldPlay = true;
                    _moveRightVideoPlayer.Stop();
                    if (_moveRightVideoPlayer.isPrepared)
                    {
                        _moveRightVideoPlayer.Play();
                    }
                    else
                    {
                        _moveRightVideoPlayer.Prepare();
                    }
                }
                else
                {
                    _moveRightVideoShouldPlay = false;
                    _moveRightVideoPlayer.Stop();
                    _moveRightVideoImage.enabled = false;
                    _leftText.gameObject.SetActive(true);

                    if (leftImage != null)
                        leftImage.enabled = true;
                }
            }
        }

        private void SetupMoveRightVideo()
        {
            GameObject videoObj = new GameObject("MoveRightVideo");
            videoObj.transform.SetParent(_leftIndicator, false);

            RectTransform rt = videoObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);

            _moveRightVideoImage = videoObj.AddComponent<RawImage>();
            _moveRightVideoImage.raycastTarget = false;

            _moveRightAspectFitter = videoObj.AddComponent<AspectRatioFitter>();
            _moveRightAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _moveRightAspectFitter.aspectRatio = 540f / 351f;

            _moveRightVideoPlayer = videoObj.AddComponent<VideoPlayer>();
            _moveRightVideoPlayer.playOnAwake = false;
            _moveRightVideoPlayer.isLooping = true;
            _moveRightVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _moveRightVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _moveRightVideoPlayer.playOnAwake = false;
            _moveRightVideoPlayer.waitForFirstFrame = true;
            _moveRightVideoPlayer.skipOnDrop = true;
            _moveRightVideoPlayer.prepareCompleted += OnMoveRightVideoPrepared;
            _moveRightVideoPlayer.errorReceived += OnMoveRightVideoError;
            _moveRightVideoPlayer.loopPointReached += OnMoveRightVideoLoopPointReached;

            VideoClip clip = Resources.Load<VideoClip>("animations/moveRightWarning");
            if (clip != null)
            {
                _moveRightVideoPlayer.source = VideoSource.VideoClip;
                _moveRightVideoPlayer.clip = clip;
            }
            else
            {
                string filePath = ResolveMoveRightWarningFilePath();
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogWarning(
                        "[G3D] moveRightWarning.mp4 not found (VideoClip or file path). Using text fallback."
                    );
                    Destroy(videoObj);
                    return;
                }

                _moveRightVideoPlayer.source = VideoSource.Url;
                _moveRightVideoPlayer.url = new Uri(filePath).AbsoluteUri;
            }

            CreateOrResizeMoveRightRenderTexture(
                clip != null ? (int)Mathf.Max(64, clip.width > 0 ? clip.width : 540) : 540,
                clip != null ? (int)Mathf.Max(64, clip.height > 0 ? clip.height : 351) : 351
            );
            _moveRightVideoImage.enabled = false;

            _hasMoveRightVideo = true;
            _moveRightVideoPlayer.Prepare();
        }

        private string ResolveMoveRightWarningFilePath()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            string[] candidates =
            {
                Path.Combine(
                    projectRoot,
                    "Packages",
                    "com.3dglobal.core",
                    "Resources",
                    "animations",
                    "moveRightWarning.mp4"
                ),
                Path.Combine(projectRoot, "Resources", "animations", "moveRightWarning.mp4"),
                Path.Combine(
                    projectRoot,
                    "Assets",
                    "Resources",
                    "animations",
                    "moveRightWarning.mp4"
                )
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                    return candidates[i];
            }

            return string.Empty;
        }

        private void OnMoveRightVideoPrepared(VideoPlayer source)
        {
            int preparedWidth = (int)Mathf.Max(64, source.width > 0 ? source.width : 540);
            int preparedHeight = (int)Mathf.Max(64, source.height > 0 ? source.height : 351);
            CreateOrResizeMoveRightRenderTexture(preparedWidth, preparedHeight);

            if (_moveRightAspectFitter != null)
                _moveRightAspectFitter.aspectRatio = preparedWidth / (float)preparedHeight;

            source.isLooping = true;
            if (_moveRightVideoShouldPlay)
                source.Play();
        }

        private void OnMoveRightVideoLoopPointReached(VideoPlayer source)
        {
            // Some codecs/platforms ignore isLooping in URL mode; force replay.
            if (_moveRightVideoShouldPlay)
                source.Play();
        }

        private void OnMoveRightVideoError(VideoPlayer source, string message)
        {
            Debug.LogError("[G3D] moveRightWarning video error: " + message);
        }

        private void CreateOrResizeMoveRightRenderTexture(int width, int height)
        {
            width = Mathf.Max(64, width);
            height = Mathf.Max(64, height);

            if (
                _moveRightRenderTexture != null
                && _moveRightRenderTexture.width == width
                && _moveRightRenderTexture.height == height
            )
            {
                _moveRightVideoPlayer.targetTexture = _moveRightRenderTexture;
                _moveRightVideoImage.texture = _moveRightRenderTexture;
                return;
            }

            if (_moveRightRenderTexture != null)
            {
                _moveRightRenderTexture.Release();
                Destroy(_moveRightRenderTexture);
            }

            _moveRightRenderTexture = new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32
            );
            _moveRightRenderTexture.Create();

            _moveRightVideoPlayer.targetTexture = _moveRightRenderTexture;
            _moveRightVideoImage.texture = _moveRightRenderTexture;
        }

        private static void SetIndicatorAlpha(RectTransform indicator, float alpha)
        {
            Image img = indicator.GetComponent<Image>();
            if (img == null)
                return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }

        private void BuildUi()
        {
            _root = new GameObject("PositionWarningOverlayRoot");
            _root.transform.SetParent(transform, false);

            _canvas = _root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            _root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler
                .ScaleMode
                .ScaleWithScreenSize;
            _root.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            _overlayFade = CreateImage("Fade", rootRect, new Color(0f, 0f, 0f, 0.1f));
            StretchToParent(_overlayFade.rectTransform);

            _topIndicator = CreateIndicator(
                "TopIndicator",
                rootRect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -120f),
                new Vector2(640f, 120f),
                "MOVE DOWN"
            );

            _bottomIndicator = CreateIndicator(
                "BottomIndicator",
                rootRect,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 120f),
                new Vector2(640f, 120f),
                "MOVE UP"
            );

            _leftIndicator = CreateIndicator(
                "LeftIndicator",
                rootRect,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(120f, 0f),
                new Vector2(420f, 120f),
                "MOVE RIGHT"
            );

            _rightIndicator = CreateIndicator(
                "RightIndicator",
                rootRect,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-260f, 0f),
                new Vector2(420f, 120f),
                "MOVE LEFT"
            );

            _centerIndicator = CreateIndicator(
                "CenterIndicator",
                rootRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 140f),
                "MOVE AWAY"
            );

            _topText = _topIndicator.GetComponentInChildren<Text>(true);
            _bottomText = _bottomIndicator.GetComponentInChildren<Text>(true);
            _leftText = _leftIndicator.GetComponentInChildren<Text>(true);
            _rightText = _rightIndicator.GetComponentInChildren<Text>(true);
            _centerText = _centerIndicator.GetComponentInChildren<Text>(true);
        }

        private RectTransform CreateIndicator(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            string text
        )
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.96f, 0.62f, 0.12f, 0.95f);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(rt, false);
            Text label = textObj.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.fontSize = warningFontSize;
            label.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 12f);
            textRt.offsetMax = new Vector2(-16f, -12f);

            return rt;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
