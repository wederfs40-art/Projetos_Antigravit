using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Controlador de Câmera Corporal Tática (Bodycam / Unrecord Style).
    /// Simula uma câmera fixada no colete tático do operador:
    /// 1. Deadzone / Free Aim: a arma se move levemente antes da câmera girar.
    /// 2. Inércia física de cabeça e tronco (Body Inertia & Momentum).
    /// 3. Tremor realista de passos com aceleração física (Tactical Vest Bobbing).
    /// 4. Vinheta de lente grande angular e visual de câmera de segurança policial.
    /// </summary>
    public class BodycamCameraController : MonoBehaviour
    {
        [Header("Modo de Visão")]
        [Tooltip("Habilita a física de câmera corporal realista estilo Bodycam / Unrecord.")]
        public bool EnableBodycamMode = true;

        [Header("Sensibilidade e Inércia")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float bodyFollowSpeed = 8.0f;
        [SerializeField] private float freeAimAngleLimit = 4.0f; // Graus de movimentação livre da arma

        [Header("Balanço de Movimentação (Vest Bobbing)")]
        [SerializeField] private float bobSpeed = 9.0f;
        [SerializeField] private float bobAmountX = 0.025f;
        [SerializeField] private float bobAmountY = 0.035f;
        [SerializeField] private float bobTiltAngle = 1.2f;

        [Header("Referências")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Camera cam;

        private float _cameraPitch = 0f;
        private float _cameraYaw = 0f;
        private float _freeAimX = 0f;
        private float _freeAimY = 0f;
        private float _bobTimer = 0f;

        private Vector3 _camOriginalLocalPos;
        private Texture2D _vignetteTex;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) _camOriginalLocalPos = cam.transform.localPosition;

            CreateBodycamVignette();
        }

        private void Start()
        {
            if (playerBody == null && transform.parent != null)
            {
                playerBody = transform.parent;
            }

            if (weaponHolder == null)
            {
                GameObject holder = GameObject.Find("WeaponHolder");
                if (holder != null) weaponHolder = holder.transform;
            }

            _cameraYaw = (playerBody != null) ? playerBody.eulerAngles.y : transform.eulerAngles.y;
            _cameraPitch = 0f;
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (EnableBodycamMode)
            {
                // ==========================================
                // 1. FREE AIM (A arma aponta antes do corpo)
                // ==========================================
                _freeAimX += mouseX;
                _freeAimY -= mouseY;

                // Restringe o desvio livre da arma
                _freeAimX = Mathf.Clamp(_freeAimX, -freeAimAngleLimit, freeAimAngleLimit);
                _freeAimY = Mathf.Clamp(_freeAimY, -freeAimAngleLimit, freeAimAngleLimit);

                // O excedente do mouse rotaciona o corpo do operador
                float excessX = mouseX - (_freeAimX == freeAimAngleLimit || _freeAimX == -freeAimAngleLimit ? 0 : mouseX);
                float excessY = mouseY - (_freeAimY == freeAimAngleLimit || _freeAimY == -freeAimAngleLimit ? 0 : mouseY);

                _cameraYaw += mouseX * 0.75f;
                _cameraPitch = Mathf.Clamp(_cameraPitch - (mouseY * 0.75f), -80f, 80f);

                // Rotaciona o corpo com inércia física
                Quaternion targetRot = Quaternion.Euler(0f, _cameraYaw, 0f);
                if (playerBody != null)
                {
                    playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetRot, bodyFollowSpeed * Time.deltaTime);
                }

                // Rotaciona a câmera verticalmente com leve inclinação lateral
                float moveX = Input.GetAxis("Horizontal");
                float tilt = -moveX * bobTiltAngle;
                transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, tilt);

                // Aplica o Free Aim na arma integrado ao sistema de viewmodel motion
                if (weaponHolder != null)
                {
                    var vm = weaponHolder.GetComponent<IPSCSimulator.Weapons.ProceduralViewmodelMotion>();
                    if (vm != null)
                    {
                        vm.SetFreeAimOffset(_freeAimX, _freeAimY);
                    }
                    else
                    {
                        Quaternion freeAimRot = Quaternion.Euler(_freeAimY, _freeAimX, 0f);
                        weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, freeAimRot, 18f * Time.deltaTime);
                    }
                }

                // ==========================================
                // 2. VEST BOBBING (Tremor de passos no colete)
                // ==========================================
                float moveSpeed = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
                if (moveSpeed > 0.1f)
                {
                    _bobTimer += Time.deltaTime * bobSpeed;
                    float waveX = Mathf.Cos(_bobTimer * 0.5f) * bobAmountX;
                    float waveY = Mathf.Sin(_bobTimer) * bobAmountY;
                    transform.localPosition = _camOriginalLocalPos + new Vector3(waveX, waveY, 0f);
                }
                else
                {
                    _bobTimer = 0f;
                    transform.localPosition = Vector3.Lerp(transform.localPosition, _camOriginalLocalPos, 6f * Time.deltaTime);
                }
            }
            else
            {
                // Modo FPS clássico
                _cameraYaw += mouseX;
                _cameraPitch = Mathf.Clamp(_cameraPitch - mouseY, -85f, 85f);

                if (playerBody != null) playerBody.rotation = Quaternion.Euler(0f, _cameraYaw, 0f);
                transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
        }

        private void CreateBodycamVignette()
        {
            int size = 256;
            _vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    // Curva de lente de câmera corporal grande angular (vinheta escura nas bordas)
                    float alpha = Mathf.Clamp01(Mathf.Pow(dist, 2.8f) * 0.75f);
                    _vignetteTex.SetPixel(x, y, new Color(0.02f, 0.02f, 0.03f, alpha));
                }
            }
            _vignetteTex.Apply();
        }

        private void OnGUI()
        {
            if (!EnableBodycamMode || _vignetteTex == null) return;

            // Desenha a vinheta de lente de câmera corporal nas bordas da tela
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignetteTex, ScaleMode.StretchToFill);

            // Carimbo de data/hora oficial no canto superior direito estilo Bodycam policial
            GUIStyle stampStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            };
            stampStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.70f);

            string timeStamp = $"REC ● BODYCAM-04  |  {System.DateTime.Now:yyyy-MM-dd  HH:mm:ss}";
            GUI.Label(new Rect(Screen.width - 320f, 15f, 300f, 22f), timeStamp, stampStyle);
        }
    }
}
