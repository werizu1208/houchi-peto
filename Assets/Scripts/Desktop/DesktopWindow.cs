using System;
using System.Collections;
using UnityEngine;

namespace HouchiPeto.Desktop
{
    /// <summary>
    /// Unity のウィンドウを、枠なし・背景透明・最前面・タスクバー非表示の小さなウィンドウにする。
    /// ペトの上にカーソルがないときはクリックを下のアプリへ素通りさせる。
    /// ビルドした Windows アプリでのみ動作し、エディタでは何もしない。
    /// </summary>
    public class DesktopWindow : MonoBehaviour
    {
        /// <summary>ウィンドウの操作ができる状態か（エディタでは常に false）</summary>
        public bool IsReady { get; private set; }

        /// <summary>ウィンドウの大きさ（物理ピクセル）</summary>
        public Vector2Int Size { get; private set; }

        /// <summary>
        /// ウィンドウ内の座標（Unity のスクリーン座標）を受け取り、そこにペトがいれば true を返す関数。
        /// クリックを素通りさせるかどうかの判定に使う。
        /// </summary>
        public Func<Vector2, bool> HitTest;

#pragma warning disable 0067 // エディタでは発火しない
        /// <summary>ペトの上で左クリックされた瞬間に呼ばれる</summary>
        public event Action Clicked;
#pragma warning restore 0067

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private IntPtr _hwnd = IntPtr.Zero;
        private bool _clickThrough;
        private bool _wasMouseDown;
        private float _topmostTimer;
#endif

        public void Init(int width, int height)
        {
            Size = new Vector2Int(width, height);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            StartCoroutine(Setup());
#endif
        }

        /// <summary>ウィンドウの左上をデスクトップ座標 (x, y) に動かす</summary>
        public void MoveTo(int x, int y)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!IsReady) return;
            Win32.SetWindowPos(_hwnd, Win32.HWND_TOPMOST, x, y, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private IEnumerator Setup()
        {
            // 解像度の変更が反映されるまで数フレーム待つ
            yield return null;
            yield return null;

            _hwnd = FindUnityWindow();
            if (_hwnd == IntPtr.Zero)
            {
                Debug.LogWarning("[DesktopWindow] Unity のウィンドウが見つかりませんでした");
                yield break;
            }

            // 枠なしにする
            Win32.SetWindowLong(_hwnd, Win32.GWL_STYLE, Win32.WS_POPUP | Win32.WS_VISIBLE);

            // タスクバーと Alt+Tab に出さない、最前面
            uint ex = Win32.GetWindowLong(_hwnd, Win32.GWL_EXSTYLE);
            ex &= ~Win32.WS_EX_APPWINDOW;
            ex |= Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST;
            Win32.SetWindowLong(_hwnd, Win32.GWL_EXSTYLE, ex);

            // クライアント領域全体に DWM のフレームを広げ、アルファ 0 の部分を透明にする
            var margins = new Win32.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            Win32.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

            Win32.SetWindowPos(_hwnd, Win32.HWND_TOPMOST, 0, 0, Size.x, Size.y,
                Win32.SWP_NOMOVE | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED | Win32.SWP_SHOWWINDOW);

            IsReady = true;
            SetClickThrough(true);
        }

        private static IntPtr FindUnityWindow()
        {
            IntPtr result = IntPtr.Zero;
            var name = new System.Text.StringBuilder(64);
            Win32.EnumThreadWindows(Win32.GetCurrentThreadId(), (hWnd, _) =>
            {
                name.Length = 0;
                Win32.GetClassName(hWnd, name, name.Capacity);
                if (name.ToString() == "UnityWndClass")
                {
                    result = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return result != IntPtr.Zero ? result : Win32.GetActiveWindow();
        }

        private void SetClickThrough(bool enable)
        {
            if (_clickThrough == enable) return;
            _clickThrough = enable;

            uint ex = Win32.GetWindowLong(_hwnd, Win32.GWL_EXSTYLE);
            if (enable) ex |= Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT;
            else ex &= ~(Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT);
            Win32.SetWindowLong(_hwnd, Win32.GWL_EXSTYLE, ex);

            // WS_EX_LAYERED を付けたら不透明度を指定しないとウィンドウが消える
            if (enable) Win32.SetLayeredWindowAttributes(_hwnd, 0, 255, Win32.LWA_ALPHA);
        }

        private void Update()
        {
            if (!IsReady) return;

            // カーソルがウィンドウ内にあるかを Win32 で調べる（素通り中は Unity にマウスイベントが来ないため）
            Win32.GetCursorPos(out var cursor);
            Win32.GetWindowRect(_hwnd, out var rect);
            bool inside = cursor.X >= rect.Left && cursor.X < rect.Right && cursor.Y >= rect.Top && cursor.Y < rect.Bottom;

            bool overPet = false;
            if (inside && HitTest != null)
            {
                // デスクトップ座標（左上原点）→ Unity のスクリーン座標（左下原点）
                var screen = new Vector2(cursor.X - rect.Left, rect.Bottom - cursor.Y);
                overPet = HitTest(screen);
            }
            SetClickThrough(!overPet);

            bool mouseDown = (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
            if (mouseDown && !_wasMouseDown && overPet) Clicked?.Invoke();
            _wasMouseDown = mouseDown;

            // 他のアプリに最前面を奪われても戻す
            _topmostTimer += Time.unscaledDeltaTime;
            if (_topmostTimer > 2f)
            {
                _topmostTimer = 0f;
                Win32.SetWindowPos(_hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0,
                    Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            }
        }
#endif
    }
}
