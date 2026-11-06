using HouchiPeto.Desktop;
using HouchiPeto.Pet;
using UnityEngine;

namespace HouchiPeto
{
    /// <summary>
    /// シーンに何も置かなくても試作が動くよう、起動時にカメラとペトを用意する。
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>ウィンドウの大きさ（物理ピクセル）。上の余白は跳ねる分</summary>
        public const int WindowWidth = 128;
        public const int WindowHeight = 128;

        /// <summary>ドット1つを何ピクセルで表示するか（16 ドットのペトが 64px になる）</summary>
        public const int PixelScale = 4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<PetWalker>() != null) return;
#else
            if (Object.FindObjectOfType<PetWalker>() != null) return;
#endif

            // ゲームの裏で動いても重くならないようにする
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            Application.runInBackground = true;

            var cam = SetupCamera();

            var windowObject = new GameObject("DesktopWindow");
            Object.DontDestroyOnLoad(windowObject);
            var window = windowObject.AddComponent<DesktopWindow>();
            window.Init(WindowWidth, WindowHeight);

            var pet = new GameObject("Peto");
            Object.DontDestroyOnLoad(pet);
            pet.AddComponent<SpriteRenderer>();
            var walker = pet.AddComponent<PetWalker>();
            walker.window = window;
            walker.viewCamera = cam;
        }

        private static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }

            // 背景をアルファ 0 にすると、その部分が透明になる
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = false;

            // 1 ユニット（= 16 ドット）を 16×PixelScale ピクセルで表示し、画面の下端を y=0 にする
            cam.orthographic = true;
            cam.orthographicSize = WindowHeight / (16f * PixelScale) / 2f;
            cam.transform.position = new Vector3(0f, cam.orthographicSize, -10f);
            return cam;
        }
    }
}
