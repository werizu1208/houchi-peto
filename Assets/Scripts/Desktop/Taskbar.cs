using UnityEngine;

namespace HouchiPeto.Desktop
{
    /// <summary>
    /// ペトが歩く「床」。デスクトップ座標（左上が原点、下向きが +Y、物理ピクセル）で表す。
    /// </summary>
    public struct WalkFloor
    {
        /// <summary>歩ける範囲の左端</summary>
        public int Left;
        /// <summary>歩ける範囲の右端</summary>
        public int Right;
        /// <summary>床の高さ。ウィンドウの下端をここに合わせる</summary>
        public int Y;
        /// <summary>タスクバーの上を歩いているか（false のときは画面の下端）</summary>
        public bool OnTaskbar;
    }

    /// <summary>
    /// タスクバーの位置から、ペトが歩く床を求める。試作ではメインモニターのみ対応。
    /// </summary>
    public static class Taskbar
    {
        public static WalkFloor GetFloor()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var data = new Win32.APPBARDATA();
            data.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32.APPBARDATA));
            bool found = Win32.SHAppBarMessage(Win32.ABM_GETTASKBARPOS, ref data) != System.IntPtr.Zero;

            if (found && data.uEdge == Win32.ABE_BOTTOM)
            {
                // タスクバーが下にあるときは、その上端を床にする
                return new WalkFloor { Left = data.rc.Left, Right = data.rc.Right, Y = data.rc.Top, OnTaskbar = true };
            }

            // タスクバーが上・左・右にあるときは、作業領域の下端（画面の下端）を床にする
            var work = new Win32.RECT();
            if (Win32.SystemParametersInfo(Win32.SPI_GETWORKAREA, 0, ref work, 0))
            {
                return new WalkFloor { Left = work.Left, Right = work.Right, Y = work.Bottom, OnTaskbar = false };
            }

            return new WalkFloor
            {
                Left = 0,
                Right = Win32.GetSystemMetrics(Win32.SM_CXSCREEN),
                Y = Win32.GetSystemMetrics(Win32.SM_CYSCREEN),
                OnTaskbar = false,
            };
#else
            // エディタでは実際のデスクトップを使わない（PetWalker 側でゲームビュー内を歩かせる）
            return new WalkFloor { Left = 0, Right = Screen.width, Y = Screen.height, OnTaskbar = false };
#endif
        }
    }
}
