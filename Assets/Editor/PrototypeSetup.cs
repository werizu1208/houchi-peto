using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HouchiPeto.EditorTools
{
    /// <summary>
    /// 透明ウィンドウに必要な Player Settings をまとめて設定する。
    /// メニュー「放置ペト > 試作用の Player Settings を適用」から実行する。
    /// </summary>
    public static class PrototypeSetup
    {
        [MenuItem("放置ペト/試作用の Player Settings を適用")]
        public static void Apply()
        {
            // flip model のスワップチェーンでは背景の透過が効かない
            PlayerSettings.useFlipModelSwapchain = false;

            // 透過は Direct3D11 で確認する
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = HouchiPeto.Bootstrap.WindowWidth;
            PlayerSettings.defaultScreenHeight = HouchiPeto.Bootstrap.WindowHeight;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;

            Debug.Log("[放置ペト] 試作用の Player Settings を適用しました");
        }
    }
}
