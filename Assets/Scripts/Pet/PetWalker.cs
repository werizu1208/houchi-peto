using HouchiPeto.Desktop;
using UnityEngine;

namespace HouchiPeto.Pet
{
    /// <summary>
    /// ペトが床（タスクバーの上）を、止まったり歩いたりしながら行き来する。
    /// ビルドではウィンドウごと動かし、エディタではゲームビューの中で絵だけ動かす。
    /// クリックすると跳ねる。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PetWalker : MonoBehaviour
    {
        [Tooltip("歩く速さ（物理ピクセル/秒）")]
        public float walkSpeed = 60f;

        [Tooltip("止まっている時間の範囲（秒）")]
        public Vector2 idleSeconds = new Vector2(1.5f, 5f);

        [Tooltip("歩き続ける時間の範囲（秒）")]
        public Vector2 walkSeconds = new Vector2(2f, 6f);

        [Tooltip("歩きのコマ送りの間隔（秒）")]
        public float frameSeconds = 0.2f;

        public DesktopWindow window;
        public Camera viewCamera;

        private enum State { Idle, Walk }

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private State _state = State.Idle;
        private float _stateTimer;
        private float _frameTimer;
        private int _frame;
        private int _direction = 1;
        private float _x;           // 床の上での位置（ビルド：ウィンドウ左端のデスクトップ x、エディタ：ワールド x）
        private WalkFloor _floor;
        private bool _desktopMode;  // ウィンドウの準備ができてデスクトップ座標で動いているか
        private float _floorTimer;
        private float _hopTime = -1f;
        private const float HopDuration = 0.45f;
        private const float HopHeight = 0.6f; // ワールド単位

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _frames = PlaceholderPetSprite.CreateWalkFrames();
            _renderer.sprite = _frames[1];
        }

        private void Start()
        {
            if (window != null)
            {
                window.HitTest = HitTest;
                window.Clicked += Hop;
            }

            _floor = Taskbar.GetFloor();
            _x = 0f;
            EnterState(State.Idle);
        }

        private bool UsesDesktop => window != null && window.IsReady;

        private void Update()
        {
            float dt = Time.deltaTime;

            // ウィンドウの準備は数フレーム遅れて終わるので、そこで床の中央へ移す
            if (UsesDesktop && !_desktopMode)
            {
                _desktopMode = true;
                _x = (_floor.Left + _floor.Right - window.Size.x) * 0.5f;
            }

            // タスクバーの移動や解像度の変更に追従する
            _floorTimer += dt;
            if (_floorTimer > 2f)
            {
                _floorTimer = 0f;
                _floor = Taskbar.GetFloor();
            }

            _stateTimer -= dt;
            if (_stateTimer <= 0f) EnterState(_state == State.Idle ? State.Walk : State.Idle);

            if (_state == State.Walk) Walk(dt);
            Animate(dt);
            ApplyPosition();

            // エディタでは左クリックで跳ねる動作を試せるようにする
#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isFocused && UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (HitTest(UnityEngine.Input.mousePosition)) Hop();
            }
#endif
        }

        private void EnterState(State next)
        {
            _state = next;
            var range = next == State.Idle ? idleSeconds : walkSeconds;
            _stateTimer = Random.Range(range.x, range.y);
            if (next == State.Walk && Random.value < 0.5f) _direction = -_direction;
        }

        private void Walk(float dt)
        {
            float min, max, speed;
            if (UsesDesktop)
            {
                min = _floor.Left;
                max = _floor.Right - window.Size.x;
                speed = walkSpeed;
            }
            else
            {
                float halfWidth = viewCamera != null ? viewCamera.orthographicSize * viewCamera.aspect : 1f;
                min = -halfWidth + 0.5f;
                max = halfWidth - 0.5f;
                speed = walkSpeed / 64f; // 1 ユニット ≒ 64px として揃える
            }

            _x += _direction * speed * dt;
            if (_x < min) { _x = min; _direction = 1; }
            if (_x > max) { _x = max; _direction = -1; }
        }

        private void Animate(float dt)
        {
            _renderer.flipX = _direction < 0;

            if (_state == State.Walk)
            {
                _frameTimer += dt;
                if (_frameTimer >= frameSeconds)
                {
                    _frameTimer = 0f;
                    _frame = 1 - _frame;
                }
                _renderer.sprite = _frames[_frame];
            }
            else
            {
                _renderer.sprite = _frames[1];
            }
        }

        private void ApplyPosition()
        {
            float hop = 0f;
            if (_hopTime >= 0f)
            {
                _hopTime += Time.deltaTime;
                float t = _hopTime / HopDuration;
                if (t >= 1f) _hopTime = -1f;
                else hop = Mathf.Sin(t * Mathf.PI) * HopHeight;
            }

            if (UsesDesktop)
            {
                // ウィンドウごと床の上に置く。絵はウィンドウの中で跳ねる
                window.MoveTo(Mathf.RoundToInt(_x), _floor.Y - window.Size.y);
                transform.localPosition = new Vector3(0f, hop, 0f);
            }
            else
            {
                transform.localPosition = new Vector3(_x, hop, 0f);
            }
        }

        private void Hop()
        {
            if (_hopTime < 0f) _hopTime = 0f;
        }

        /// <summary>スクリーン座標にペトの不透明なドットがあれば true</summary>
        private bool HitTest(Vector2 screenPoint)
        {
            if (viewCamera == null || _renderer.sprite == null) return false;

            Vector3 world = viewCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -viewCamera.transform.position.z));
            Bounds b = _renderer.bounds;
            if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y) return false;

            // 透明なドットの上ではクリックを素通りさせる
            var sprite = _renderer.sprite;
            float u = Mathf.InverseLerp(b.min.x, b.max.x, world.x);
            float v = Mathf.InverseLerp(b.min.y, b.max.y, world.y);
            if (_renderer.flipX) u = 1f - u;
            var rect = sprite.textureRect;
            int px = Mathf.Clamp((int)(rect.x + u * rect.width), 0, sprite.texture.width - 1);
            int py = Mathf.Clamp((int)(rect.y + v * rect.height), 0, sprite.texture.height - 1);
            return sprite.texture.GetPixel(px, py).a > 0.1f;
        }
    }
}
