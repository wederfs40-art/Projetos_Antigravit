using UnityEngine;

namespace IPSCSimulator.Player
{
    /// <summary>
    /// Controlador em primeira pessoa simples e responsivo para locomoção no estande de tiro.
    /// Inclui caminhar, correr e rotação da câmera (Mouse Look).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomoção")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.0f;
        [SerializeField] private float gravity = -15f;

        [Header("Olhar (Câmera)")]
        [SerializeField] private Transform playerCameraTransform;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxPitchAngle = 85f;
        [SerializeField] private float minPitchAngle = -85f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _cameraPitch;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleCameraLook();
            HandleMovement();
            HandleCursorToggle();
        }

        private void HandleCameraLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            // Rotação horizontal do corpo
            transform.Rotate(Vector3.up * mouseX);

            // Rotação vertical da câmera
            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitchAngle, maxPitchAngle);

            if (playerCameraTransform != null)
            {
                playerCameraTransform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = (transform.forward * vertical + transform.right * horizontal).normalized;

            // Corrida apenas com Shift se não estiver mirando (pode ser ajustado)
            bool isSprinting = Input.GetKey(KeyCode.LeftControl);
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // Gravidade simples
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 moveVelocity = direction * currentSpeed;
            moveVelocity.y = _verticalVelocity;

            _characterController.Move(moveVelocity * Time.deltaTime);
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }
        }
    }
}
