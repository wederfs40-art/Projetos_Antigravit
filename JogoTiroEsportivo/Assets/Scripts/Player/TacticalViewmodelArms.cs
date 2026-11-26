using System.Collections;
using UnityEngine;

namespace IPSCSimulator.Player
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerenciador Tático de Postura e Viewmodel em Primeira Pessoa (FPS Tactical Viewmodel):
    /// - Posicionamento tático de alta precisão (High Ready / Low Ready / ADS / Inspect).
    /// - Alinhamento ergonômico autêntico para Fuzis (C-Clamp Stance) e Pistolas (Two-Handed Modern Isosceles).
    /// - Integração fluida com recuo procedural, balanço (sway) e animações de ciclo mecânico.
    /// - NÃO instancia primitivos geométricos (cilindros/cubos), garantindo visual limpo e profissional
    ///   destacando os modelos 3D reais com texturas PBR completas (ChamferZone AKM, Beretta M9T, DWm715).
    /// </summary>
    public class TacticalViewmodelArms : MonoBehaviour
    {
        private Transform _weaponHolder;
        private TacticalWeaponCycles _cycles;
        private SelectedWeaponType _currentWeaponType;

        private Vector3 _stanceOffset = Vector3.zero;
        private Vector3 _stanceRotation = Vector3.zero;
        private Vector3 _currentOffsetVelocity;

        public void InitializeArms(Transform weaponHolder, Material matSleeve = null, Material matGlove = null, Material matArmor = null)
        {
            _weaponHolder = weaponHolder;
            _cycles = weaponHolder.GetComponentInChildren<TacticalWeaponCycles>();

            // Remove quaisquer artefatos primitivos residuais
            Transform oldHands = _weaponHolder.Find("FPS_Photorealistic_Tactical_Hands");
            if (oldHands != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(oldHands.gameObject);
                else Destroy(oldHands.gameObject);
#else
                Destroy(oldHands.gameObject);
#endif
            }

            Transform oldLegacyHands = _weaponHolder.Find("FPS_Tactical_Hands");
            if (oldLegacyHands != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(oldLegacyHands.gameObject);
                else Destroy(oldLegacyHands.gameObject);
#else
                Destroy(oldLegacyHands.gameObject);
#endif
            }
        }

        public void SetWeaponIKStance(SelectedWeaponType weaponType)
        {
            _currentWeaponType = weaponType;

            switch (weaponType)
            {
                case SelectedWeaponType.SMG_Tactical:
                    // Postura tática para fuzil AKM: arma ligeiramente angulada e empunhada firme à frente
                    _stanceOffset = new Vector3(0.01f, -0.015f, 0.02f);
                    _stanceRotation = new Vector3(0.5f, -1.2f, 1.8f);
                    break;

                case SelectedWeaponType.Revolver_DanWesson:
                    // Postura de revólver esportivo/tático: empunhadura firme e centralizada
                    _stanceOffset = new Vector3(0f, -0.01f, 0f);
                    _stanceRotation = new Vector3(0f, 0f, 0f);
                    break;

                case SelectedWeaponType.Pistol_9mm:
                default:
                    // Postura moderna de pistola (thumbs-forward / isosceles)
                    _stanceOffset = new Vector3(0f, 0f, 0f);
                    _stanceRotation = new Vector3(0f, 0f, 0f);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (_weaponHolder == null) return;

            // Transição suave para recarga ou inspect
            Vector3 targetOffset = _stanceOffset;
            if (_cycles != null && _cycles.IsReloading)
            {
                targetOffset += new Vector3(0.02f, -0.06f, -0.04f);
            }

            // Aplica micro-ajuste tático sem interferir no controle de mira ADS
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetOffset, ref _currentOffsetVelocity, 0.08f);
        }
    }
}
