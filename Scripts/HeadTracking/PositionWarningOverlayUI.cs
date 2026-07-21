using System;
using System.Collections.Generic;
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

        private GameObject _root;
        private Image _overlayFade;

        private RectTransform _topIndicator;
        private RectTransform _bottomIndicator;
        private RectTransform _leftIndicator;
        private RectTransform _rightIndicator;
        private RectTransform _centerIndicator;
        private RectTransform[] _indicators;

        private Text _topText;
        private Text _bottomText;
        private Text _leftText;
        private Text _rightText;
        private Text _centerText;

        private sealed class VideoHint
        {
            public WarnSide Side;
            public string ResourceName;
            public RectTransform Indicator;
            public Text Label;
            public RawImage Image;
            public VideoPlayer Player;
            public RenderTexture RenderTexture;
            public AspectRatioFitter AspectFitter;
            public bool HasVideo;
            public bool ShouldPlay;
        }

        private readonly List<VideoHint> _videoHints = new List<VideoHint>();

        private WarnSide _currentSide = WarnSide.None;
        private float _sideShownAt = -1f;
        private float _hideAt = -1f;

        private void Awake()
        {
            BuildUi();
            SetupAllWarningVideos();
            HideAllNow();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _videoHints.Count; i++)
            {
                VideoHint hint = _videoHints[i];
                if (hint.Player != null)
                {
                    hint.Player.Stop();
                    hint.Player.targetTexture = null;
                    hint.Player.prepareCompleted -= OnAnyVideoPrepared;
                    hint.Player.errorReceived -= OnAnyVideoError;
                    hint.Player.loopPointReached -= OnAnyVideoLoopPointReached;
                }

                if (hint.RenderTexture != null)
                {
                    hint.RenderTexture.Release();
                    Destroy(hint.RenderTexture);
                    hint.RenderTexture = null;
                }
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

                _root.SetActive(true);
                SetActiveOnly(side);
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
            VideoHint activeVideoHint = GetActiveVideoHint(_currentSide);
            if (activeVideoHint != null && activeVideoHint.HasVideo)
            {
                _overlayFade.color = new Color(0f, 0f, 0f, 0.1f);
                return;
            }

            float phase = (Time.unscaledTime - _sideShownAt) / Mathf.Max(0.01f, pulseCycleSeconds);
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
            float scale = Mathf.Lerp(0.96f, 1.05f, pulse);
            float alpha = Mathf.Lerp(0.45f, 1f, pulse);

            RectTransform active = GetIndicatorForSide(_currentSide);
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

        private RectTransform GetIndicatorForSide(WarnSide side)
        {
            switch (side)
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

            if (_indicators != null)
            {
                foreach (RectTransform indicator in _indicators)
                    indicator.gameObject.SetActive(false);
            }

            SetAllVideoHintsInactive();
        }

        private void SetActiveOnly(WarnSide side)
        {
            RectTransform activeIndicator = GetIndicatorForSide(side);
            foreach (RectTransform indicator in _indicators)
            {
                indicator.gameObject.SetActive(indicator == activeIndicator);
                indicator.localScale = Vector3.one;
                SetIndicatorAlpha(indicator, 1f);
            }

            // Only the depth cue changes wording; the edge labels are set once in BuildUi.
            _centerText.text = side == WarnSide.Close ? "MOVE AWAY" : "MOVE CLOSER";

            SetAllVideoHintsInactive();
            ActivateVideoHintForSide(side);
        }

        private void SetupAllWarningVideos()
        {
            _videoHints.Clear();
            TryCreateVideoHint(WarnSide.Top, "moveDownWarning", _topIndicator, _topText);
            TryCreateVideoHint(WarnSide.Bottom, "moveUpWarning", _bottomIndicator, _bottomText);
            TryCreateVideoHint(WarnSide.Left, "moveRightWarning", _leftIndicator, _leftText);
            TryCreateVideoHint(WarnSide.Right, "moveLeftWarning", _rightIndicator, _rightText);
            TryCreateVideoHint(WarnSide.Close, "moveAwayWarning", _centerIndicator, _centerText);
            TryCreateVideoHint(WarnSide.Far, "moveCloserWarning", _centerIndicator, _centerText);
        }

        private void TryCreateVideoHint(
            WarnSide side,
            string resourceName,
            RectTransform indicator,
            Text label
        )
        {
            VideoHint hint = new VideoHint
            {
                Side = side,
                ResourceName = resourceName,
                Indicator = indicator,
                Label = label
            };

            GameObject videoObj = new GameObject(resourceName + "Video");
            videoObj.transform.SetParent(indicator, false);

            RectTransform rt = videoObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);

            hint.Image = videoObj.AddComponent<RawImage>();
            hint.Image.raycastTarget = false;
            hint.Image.enabled = false;

            hint.AspectFitter = videoObj.AddComponent<AspectRatioFitter>();
            hint.AspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            hint.AspectFitter.aspectRatio = 540f / 351f;

            hint.Player = videoObj.AddComponent<VideoPlayer>();
            hint.Player.playOnAwake = false;
            hint.Player.isLooping = true;
            hint.Player.audioOutputMode = VideoAudioOutputMode.None;
            hint.Player.renderMode = VideoRenderMode.RenderTexture;
            hint.Player.waitForFirstFrame = true;
            hint.Player.skipOnDrop = true;
            hint.Player.prepareCompleted += OnAnyVideoPrepared;
            hint.Player.errorReceived += OnAnyVideoError;
            hint.Player.loopPointReached += OnAnyVideoLoopPointReached;

            // Prefer Unity's imported VideoClip: it is transcoded into the Library
            // and plays reliably even when the raw file in a package can't be read
            // directly. Fall back to a validated on-disk .mp4 only if no clip exists.
            VideoClip clip = Resources.Load<VideoClip>("animations/" + resourceName);
            if (clip != null)
            {
                hint.Player.source = VideoSource.VideoClip;
                hint.Player.clip = clip;
                CreateOrResizeRenderTexture(
                    hint,
                    (int)Mathf.Max(64, clip.width),
                    (int)Mathf.Max(64, clip.height)
                );
                hint.HasVideo = true;
                _videoHints.Add(hint);
                return;
            }

            string filePath = ResolveWarningFilePath(resourceName + ".mp4");
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning(
                    "[G3D] " + resourceName + ".mp4 not found or invalid. Using text fallback."
                );
                Destroy(videoObj);
                _videoHints.Add(hint);
                return;
            }

            hint.Player.source = VideoSource.Url;
            hint.Player.url = new Uri(filePath).AbsoluteUri;
            CreateOrResizeRenderTexture(hint, 540, 351);

            hint.HasVideo = true;
            _videoHints.Add(hint);
        }

        private string ResolveWarningFilePath(string fileName)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            // Prefer project-level assets first; package path is a fallback.
            string[] candidates =
            {
                Path.Combine(projectRoot, "Assets", "Resources", "animations", fileName),
                Path.Combine(projectRoot, "Resources", "animations", fileName),
                Path.Combine(
                    projectRoot,
                    "Packages",
                    "com.3dglobal.core",
                    "Resources",
                    "animations",
                    fileName
                )
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (!File.Exists(candidate))
                    continue;

                if (IsPlayableMp4(candidate))
                    return candidate;
            }

            return string.Empty;
        }

        // Guards against files that exist but aren't real video: 0-byte files and
        // Git-LFS pointer stubs (a few hundred bytes of text) both make the native
        // player log "received empty file" / "Cannot read file". Detect and skip them
        // so we fall back to the text hint cleanly instead of spamming errors.
        private static bool IsPlayableMp4(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Length < 1024)
                {
                    Debug.LogWarning("[G3D] Ignoring empty/too-small video file: " + path);
                    return false;
                }

                using (FileStream stream = File.OpenRead(path))
                {
                    byte[] header = new byte[16];
                    int read = stream.Read(header, 0, header.Length);
                    if (read < 12)
                        return false;

                    // Real MP4/QuickTime files carry an 'ftyp' box near the start.
                    bool hasFtyp =
                        header[4] == (byte)'f'
                        && header[5] == (byte)'t'
                        && header[6] == (byte)'y'
                        && header[7] == (byte)'p';

                    // Git-LFS pointer files begin with the ASCII text "version ".
                    bool isLfsPointer =
                        header[0] == (byte)'v'
                        && header[1] == (byte)'e'
                        && header[2] == (byte)'r'
                        && header[3] == (byte)'s';

                    if (isLfsPointer || !hasFtyp)
                    {
                        Debug.LogWarning(
                            "[G3D] Ignoring non-video file (empty, corrupt, or Git-LFS pointer): "
                                + path
                        );
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[G3D] Ignoring unreadable warning video file: "
                        + path
                        + " ("
                        + ex.Message
                        + ")"
                );
                return false;
            }
        }

        private void OnAnyVideoPrepared(VideoPlayer source)
        {
            VideoHint hint = FindHintByPlayer(source);
            if (hint == null)
                return;

            int preparedWidth = (int)Mathf.Max(64, source.width > 0 ? source.width : 540);
            int preparedHeight = (int)Mathf.Max(64, source.height > 0 ? source.height : 351);
            CreateOrResizeRenderTexture(hint, preparedWidth, preparedHeight);

            if (hint.AspectFitter != null)
                hint.AspectFitter.aspectRatio = preparedWidth / (float)preparedHeight;

            source.isLooping = true;
            if (hint.ShouldPlay)
                source.Play();
        }

        private void OnAnyVideoLoopPointReached(VideoPlayer source)
        {
            // Some codecs/platforms ignore isLooping in URL mode; force replay.
            VideoHint hint = FindHintByPlayer(source);
            if (hint != null && hint.ShouldPlay)
                source.Play();
        }

        private void OnAnyVideoError(VideoPlayer source, string message)
        {
            VideoHint hint = FindHintByPlayer(source);
            string id = hint != null ? hint.ResourceName : "unknownWarning";
            Debug.LogError("[G3D] " + id + " video error: " + message);
        }

        private void CreateOrResizeRenderTexture(VideoHint hint, int width, int height)
        {
            width = Mathf.Max(64, width);
            height = Mathf.Max(64, height);

            if (
                hint.RenderTexture != null
                && hint.RenderTexture.width == width
                && hint.RenderTexture.height == height
            )
            {
                hint.Player.targetTexture = hint.RenderTexture;
                hint.Image.texture = hint.RenderTexture;
                return;
            }

            if (hint.RenderTexture != null)
            {
                hint.RenderTexture.Release();
                Destroy(hint.RenderTexture);
            }

            hint.RenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            hint.RenderTexture.Create();

            hint.Player.targetTexture = hint.RenderTexture;
            hint.Image.texture = hint.RenderTexture;
        }

        private VideoHint FindHintByPlayer(VideoPlayer player)
        {
            for (int i = 0; i < _videoHints.Count; i++)
            {
                if (_videoHints[i].Player == player)
                    return _videoHints[i];
            }

            return null;
        }

        private VideoHint GetActiveVideoHint(WarnSide side)
        {
            for (int i = 0; i < _videoHints.Count; i++)
            {
                if (_videoHints[i].Side == side && _videoHints[i].HasVideo)
                    return _videoHints[i];
            }

            return null;
        }

        private void SetAllVideoHintsInactive()
        {
            for (int i = 0; i < _videoHints.Count; i++)
            {
                VideoHint hint = _videoHints[i];
                if (!hint.HasVideo)
                    continue;

                hint.ShouldPlay = false;
                hint.Player.Stop();
                hint.Image.enabled = false;

                Image background = hint.Indicator.GetComponent<Image>();
                if (background != null)
                    background.enabled = true;

                if (hint.Label != null)
                    hint.Label.gameObject.SetActive(true);
            }
        }

        private void ActivateVideoHintForSide(WarnSide side)
        {
            VideoHint hint = GetActiveVideoHint(side);
            if (hint == null || !hint.HasVideo)
                return;

            hint.Image.enabled = true;
            hint.ShouldPlay = true;

            if (hint.Label != null)
                hint.Label.gameObject.SetActive(false);

            Image background = hint.Indicator.GetComponent<Image>();
            if (background != null)
                background.enabled = false;

            hint.Player.Stop();
            if (hint.Player.isPrepared)
                hint.Player.Play();
            else
                hint.Player.Prepare();
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

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
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
                new Vector2(0f, -90f),
                new Vector2(420f, 120f),
                "MOVE DOWN"
            );

            _bottomIndicator = CreateIndicator(
                "BottomIndicator",
                rootRect,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 90f),
                new Vector2(420f, 120f),
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
                new Vector2(-120f, 0f),
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

            _indicators = new[]
            {
                _topIndicator,
                _bottomIndicator,
                _leftIndicator,
                _rightIndicator,
                _centerIndicator
            };
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
