using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IceFishing.EditorTools
{
    /// <summary>
    /// 竖屏 1080×2160。修正 Game/Play 视图默认 minScale=1.5 导致一进 Play 缩放被拉到 1.5 的问题。
    /// 菜单：IceFishing / Game View 9:18。
    /// </summary>
    [InitializeOnLoad]
    public static class IceFishingGameView
    {
        const string PlayerSettingsKey = "IceFishing.PlayerSettings.918";
        const string SizeName = "1080x2160 (9:18)";
        const int PixelW = 1080;
        const int PixelH = 2160;
        const float MinZoom = 0.05f;
        const float MaxZoom = 8f;

        static readonly Type GameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        static readonly Type PlayModeViewType = typeof(Editor).Assembly.GetType("UnityEditor.PlayModeView");

        static float _zoomBeforePlay = 1f;
        static int _restoreFramesLeft;

        static IceFishingGameView()
        {
            EditorApplication.delayCall += OnEditorReady;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += TickRestoreZoom;
        }

        [MenuItem("IceFishing/Game View 9:18")]
        public static void ApplyFromMenu()
        {
            ApplyPortrait(selectGameViewSize: true);
        }

        static void OnEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            RelaxAllViewMinScales();
            if (!SessionState.GetBool(PlayerSettingsKey, false))
            {
                SessionState.SetBool(PlayerSettingsKey, true);
                ApplyPortrait(selectGameViewSize: false);
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                _zoomBeforePlay = TryGetZoomScale(FindPrimaryView());
                if (_zoomBeforePlay < 0.01f)
                {
                    _zoomBeforePlay = 1f;
                }

                _restoreFramesLeft = 60;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                _restoreFramesLeft = 0;
            }
        }

        static void TickRestoreZoom()
        {
            // Keep minScale/maxScale relaxed so Unity does not snap back to 1.5/8 when entering Play.
            // Do NOT force a custom zoom — let the Game View fit the window naturally.
            RelaxAllViewMinScales();
        }

        static void ForceZoomNow()
        {
            RelaxAllViewMinScales();
        }

        static void ApplyPortrait(bool selectGameViewSize)
        {
            try
            {
                PlayerSettings.defaultScreenWidth = PixelW;
                PlayerSettings.defaultScreenHeight = PixelH;
                PlayerSettings.defaultIsNativeResolution = false;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

                RelaxAllViewMinScales();

                if (!selectGameViewSize)
                {
                    return;
                }

                var zoomBefore = TryGetZoomScale(FindPrimaryView());
                var index = FindOrAddFixedResolution(PixelW, PixelH, SizeName);
                var gameView = GetWindow(GameViewType);
                SelectSize(gameView, index);
                ApplyZoomToAllViews(zoomBefore > 0.01f ? zoomBefore : 1f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("IceFishing Game View 9:18: " + ex.Message);
            }
        }

        static void RelaxAllViewMinScales()
        {
            foreach (var view in GetAllViews())
            {
                RelaxMinScale(view);
            }
        }

        static void RelaxMinScale(EditorWindow view)
        {
            if (view == null)
            {
                return;
            }

            var viewType = view.GetType();
            SetFloatProperty(view, viewType, "minScale", MinZoom);
            SetFloatProperty(view, viewType, "maxScale", MaxZoom);
            var defaultScale = viewType.GetField("m_defaultScale", BindingFlags.Instance | BindingFlags.NonPublic);
            if (defaultScale != null && defaultScale.FieldType == typeof(float))
            {
                defaultScale.SetValue(view, _zoomBeforePlay > 0.01f ? _zoomBeforePlay : 1f);
            }
        }

        static void ApplyZoomToAllViews(float zoom)
        {
            zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            foreach (var view in GetAllViews())
            {
                TrySetZoomScale(view, zoom);
            }
        }

        static float TryGetZoomScale(EditorWindow view)
        {
            if (view == null)
            {
                return 1f;
            }

            var area = GetZoomArea(view);
            if (area == null)
            {
                return 1f;
            }

            var scaleField = area.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scaleField == null)
            {
                return 1f;
            }

            var scale = (Vector2)scaleField.GetValue(area);
            return scale.x;
        }

        static bool TrySetZoomScale(EditorWindow view, float zoom)
        {
            if (view == null)
            {
                return false;
            }

            var area = GetZoomArea(view);
            if (area == null)
            {
                return false;
            }

            var areaType = area.GetType();
            var scaleField = areaType.GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scaleField == null)
            {
                return false;
            }

            scaleField.SetValue(area, new Vector2(zoom, zoom));

            var zoomChanged = areaType.GetMethod("UpdateZoomScale", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (zoomChanged != null)
            {
                try
                {
                    var parameters = zoomChanged.GetParameters();
                    var args = new object[parameters.Length];
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var p = parameters[i];
                        if (p.ParameterType == typeof(float))
                        {
                            args[i] = p.Name == "minScale" ? MinZoom : MaxZoom;
                        }
                        else if (p.ParameterType == typeof(bool))
                        {
                            args[i] = false;
                        }
                        else
                        {
                            args[i] = p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null);
                        }
                    }

                    zoomChanged.Invoke(area, args);
                }
                catch (TargetParameterCountException)
                {
                    // Unity upgraded UpdateZoomScale signature; fall back to reflected property + Repaint below.
                }
            }

            var aspect = areaType.GetMethod("CalcScale", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (aspect != null && aspect.GetParameters().Length == 0)
            {
                try { aspect.Invoke(area, null); } catch (Exception) { }
            }

            view.Repaint();
            return true;
        }

        static object GetZoomArea(EditorWindow view)
        {
            var field = view.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(view) : null;
        }

        static EditorWindow FindPrimaryView()
        {
            if (EditorApplication.isPlaying)
            {
                var playView = GetWindow(PlayModeViewType);
                if (playView != null)
                {
                    return playView;
                }
            }

            return GetWindow(GameViewType);
        }

        static IEnumerable<EditorWindow> GetAllViews()
        {
            var seen = new HashSet<EditorWindow>();
            foreach (var type in new[] { GameViewType, PlayModeViewType })
            {
                var view = GetWindow(type);
                if (view != null && seen.Add(view))
                {
                    yield return view;
                }
            }

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (var i = 0; i < windows.Length; i++)
            {
                var window = windows[i];
                if (window == null || seen.Contains(window))
                {
                    continue;
                }

                var type = window.GetType();
                if (type == GameViewType || type == PlayModeViewType || IsPlayModeViewType(type))
                {
                    seen.Add(window);
                    yield return window;
                }
            }
        }

        static bool IsPlayModeViewType(Type type)
        {
            return PlayModeViewType != null && (type == PlayModeViewType || type.IsSubclassOf(PlayModeViewType));
        }

        static EditorWindow GetWindow(Type viewType)
        {
            if (viewType == null)
            {
                return null;
            }

            var getMain = viewType.GetMethod("GetMainGameView", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var view = getMain != null ? getMain.Invoke(null, null) as EditorWindow : null;
            return view ?? EditorWindow.GetWindow(viewType, false, null, false);
        }

        static void SetFloatProperty(object target, Type targetType, string name, float value)
        {
            var prop = targetType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(float))
            {
                prop.SetValue(target, value, null);
            }
        }

        static object CurrentSizeGroup()
        {
            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance").GetValue(null, null);
            return sizesType.GetProperty("currentGroup").GetValue(instance, null);
        }

        static int FindOrAddFixedResolution(int width, int height, string name)
        {
            var group = CurrentSizeGroup();
            var groupType = group.GetType();
            var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            var getTotalCount = groupType.GetMethod("GetTotalCount");
            var getGameViewSize = groupType.GetMethod("GetGameViewSize");
            var widthProp = sizeType.GetProperty("width");
            var heightProp = sizeType.GetProperty("height");
            var typeProp = sizeType.GetProperty("sizeType");
            var fixedEnum = Enum.Parse(sizeTypeEnum, "FixedResolution");
            var count = (int)getTotalCount.Invoke(group, null);

            for (var i = 0; i < count; i++)
            {
                var size = getGameViewSize.Invoke(group, new object[] { i });
                if (!Equals(typeProp.GetValue(size, null), fixedEnum))
                {
                    continue;
                }

                if ((int)widthProp.GetValue(size, null) == width && (int)heightProp.GetValue(size, null) == height)
                {
                    return i;
                }
            }

            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var created = ctor.Invoke(new object[] { fixedEnum, width, height, name });
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });

            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance").GetValue(null, null);
            var save = sizesType.GetMethod("SaveToHDD");
            if (save != null)
            {
                save.Invoke(instance, null);
            }

            return (int)getTotalCount.Invoke(group, null) - 1;
        }

        static void SelectSize(EditorWindow gameView, int index)
        {
            if (gameView == null)
            {
                return;
            }

            var gameViewType = gameView.GetType();
            var callback = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (callback != null)
            {
                callback.Invoke(gameView, new object[] { index, null });
            }
            else
            {
                var selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                selected.SetValue(gameView, index, null);
            }

            gameView.Repaint();
        }
    }
}
