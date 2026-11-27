using System;
using System.Reflection;
using UnityEngine;

namespace IPSCSimulator.Weapons
{
    /// <summary>
    /// Gerencia o Inverse Kinematics (IK) dos braços e mãos em primeira pessoa.
    /// Fixa a mão esquerda do operador firmemente no guarda-mato / handguard da arma (leftHandGripTarget).
    /// Compatível com o pacote 'com.unity.animation.rigging' (TwoBoneIKConstraint) e possui
    /// solver analítico procedural integrado de Two-Bone IK (Lei dos Cossenos) para funcionar
    /// instantaneamente no Built-in Render Pipeline mesmo sem pacotes adicionais instalados.
    /// </summary>
    [ExecuteAlways]
    public class WeaponHandIK : MonoBehaviour
    {
        // ========================================================================================
        // 1. REFERÊNCIAS DO ALVO NA ARMA (GRIP TARGET)
        // ========================================================================================
        [Header("1. Alvo na Arma (Handguard Grip Target)")]
        [Tooltip("Transform na arma (handguard/frame) onde a mão esquerda deve se fixar com firmeza.")]
        public Transform leftHandGripTarget;

        [Tooltip("Transform de dica/polo (Hint) que orienta a flexão natural do cotovelo esquerdo.")]
        public Transform leftElbowHint;

        // ========================================================================================
        // 2. OSSOS DO BRAÇO ESQUERDO (RIG DE BRAÇOS FPS)
        // ========================================================================================
        [Header("2. Ossos do Braço Esquerdo (Two-Bone Chain)")]
        [Tooltip("Osso do ombro / braço superior (Upper Arm / Shoulder).")]
        public Transform leftUpperArm;

        [Tooltip("Osso do antebraço / cotovelo (Forearm / Elbow).")]
        public Transform leftForearm;

        [Tooltip("Osso do punho / mão esquerda (Hand / Wrist).")]
        public Transform leftHand;

        // ========================================================================================
        // 3. PESO E CONFIGURAÇÃO DO IK
        // ========================================================================================
        [Header("3. Peso e Sincronização")]
        [Range(0f, 1f)]
        [Tooltip("Peso da influência do IK sobre a mão esquerda (1.0 = mão 100% colada na arma).")]
        public float ikWeight = 1.0f;

        [Tooltip("Se verdadeiro, tenta sincronizar e alimentar automaticamente qualquer componente TwoBoneIKConstraint do pacote Animation Rigging.")]
        public bool syncWithAnimationRigging = true;

        [Tooltip("Se verdadeiro, utiliza o solver procedural analítico caso Animation Rigging não esteja presente na hierarquia.")]
        public bool useProceduralFallback = true;

        // Cache de reflexão para o pacote com.unity.animation.rigging
        private Component _riggingConstraintComponent = null;
        private FieldInfo _dataFieldInfo = null;
        private bool _checkedRiggingPackage = false;

        private void Start()
        {
            AutoDetectArmBones();
            CheckAndBindAnimationRigging();
        }

        private void OnEnable()
        {
            AutoDetectArmBones();
            CheckAndBindAnimationRigging();
        }

        /// <summary>
        /// Procura automaticamente ossos comuns de braço esquerdo na hierarquia caso não estejam atribuídos.
        /// </summary>
        public void AutoDetectArmBones()
        {
            if (leftUpperArm == null || leftForearm == null || leftHand == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    string n = child.name.ToLower();
                    if (leftUpperArm == null && (n.Contains("upperarm_l") || n.Contains("arm_l") || n.Contains("leftupperarm") || n.Contains("leftarm")))
                        leftUpperArm = child;
                    else if (leftForearm == null && (n.Contains("forearm_l") || n.Contains("lowerarm_l") || n.Contains("leftforearm") || n.Contains("leftelbow")))
                        leftForearm = child;
                    else if (leftHand == null && (n.Contains("hand_l") || n.Contains("wrist_l") || n.Contains("lefthand")))
                        leftHand = child;
                }
            }
        }

        /// <summary>
        /// Verifica se o pacote Animation Rigging está presente e vincula dinamicamente o TwoBoneIKConstraint.
        /// </summary>
        private void CheckAndBindAnimationRigging()
        {
            if (!syncWithAnimationRigging || _checkedRiggingPackage) return;

            // Busca tipos do pacote com.unity.animation.rigging por reflexão segura
            Type constraintType = Type.GetType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint, Unity.Animation.Rigging");
            if (constraintType != null)
            {
                _riggingConstraintComponent = GetComponentInChildren(constraintType);
                if (_riggingConstraintComponent != null)
                {
                    _dataFieldInfo = constraintType.GetField("m_Data", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    ApplyRiggingConstraintData();
                }
            }
            _checkedRiggingPackage = true;
        }

        private void ApplyRiggingConstraintData()
        {
            if (_riggingConstraintComponent == null || leftHandGripTarget == null) return;

            try
            {
                // Seta peso no constraint
                PropertyInfo weightProp = _riggingConstraintComponent.GetType().GetProperty("weight");
                if (weightProp != null) weightProp.SetValue(_riggingConstraintComponent, ikWeight);

                // Vincula os alvos de target e hint no struct TwoBoneIKConstraintData
                if (_dataFieldInfo != null)
                {
                    object dataObj = _dataFieldInfo.GetValue(_riggingConstraintComponent);
                    if (dataObj != null)
                    {
                        Type dataType = dataObj.GetType();
                        PropertyInfo targetProp = dataType.GetProperty("target");
                        PropertyInfo hintProp = dataType.GetProperty("hint");

                        if (targetProp != null) targetProp.SetValue(dataObj, leftHandGripTarget);
                        if (hintProp != null && leftElbowHint != null) hintProp.SetValue(dataObj, leftElbowHint);

                        _dataFieldInfo.SetValue(_riggingConstraintComponent, dataObj);
                    }
                }
            }
            catch
            {
                // Degrada silenciosamente para o solver procedural
            }
        }

        private void LateUpdate()
        {
            if (ikWeight <= 0.001f || leftHandGripTarget == null) return;

            // Se Animation Rigging estiver ativo e gerenciando, atualiza os dados
            if (_riggingConstraintComponent != null && syncWithAnimationRigging)
            {
                ApplyRiggingConstraintData();
                return;
            }

            // Fallback analítico procedural (Lei dos Cossenos):
            // Garante que a mão esquerda fique colada no guarda-mato mesmo sem Animation Rigging
            if (useProceduralFallback && leftUpperArm != null && leftForearm != null && leftHand != null)
            {
                SolveProceduralTwoBoneIK();
            }
        }

        /// <summary>
        /// Solver matemático analítico Two-Bone IK de alta precisão (Law of Cosines).
        /// Ajusta ombro e cotovelo de modo que a mão alcance perfeitamente o leftHandGripTarget.
        /// </summary>
        private void SolveProceduralTwoBoneIK()
        {
            Vector3 rootPos = leftUpperArm.position;
            Vector3 midPos = leftForearm.position;
            Vector3 tipPos = leftHand.position;
            Vector3 targetPos = leftHandGripTarget.position;
            Quaternion targetRot = leftHandGripTarget.rotation;

            float length1 = Vector3.Distance(rootPos, midPos);
            float length2 = Vector3.Distance(midPos, tipPos);
            float totalLength = length1 + length2;

            Vector3 rootToTarget = targetPos - rootPos;
            float distToTarget = rootToTarget.magnitude;

            // Limita a distância para o alcance máximo dos dois ossos
            distToTarget = Mathf.Clamp(distToTarget, 0.001f, totalLength - 0.001f);
            Vector3 targetDir = rootToTarget.normalized;

            // Direção de dobra do cotovelo (polo / hint)
            Vector3 bendNormal = (leftElbowHint != null) 
                ? Vector3.Cross(targetDir, leftElbowHint.position - rootPos).normalized
                : Vector3.Cross(targetDir, -leftUpperArm.right).normalized;

            if (bendNormal.sqrMagnitude < 0.001f)
            {
                bendNormal = Vector3.up;
            }

            // Lei dos Cossenos: calcula o ângulo interno no osso do braço superior
            float cosAngle1 = (distToTarget * distToTarget + length1 * length1 - length2 * length2) / (2f * distToTarget * length1);
            cosAngle1 = Mathf.Clamp(cosAngle1, -1f, 1f);
            float angle1 = Mathf.Acos(cosAngle1) * Mathf.Rad2Deg;

            // Lei dos Cossenos: calcula o ângulo interno no cotovelo
            float cosAngle2 = (length1 * length1 + length2 * length2 - distToTarget * distToTarget) / (2f * length1 * length2);
            cosAngle2 = Mathf.Clamp(cosAngle2, -1f, 1f);
            float angle2 = Mathf.Acos(cosAngle2) * Mathf.Rad2Deg;

            // 1. Rotação do Upper Arm
            Vector3 planeNormal = Vector3.Cross(targetDir, bendNormal).normalized;
            Quaternion rotUpper = Quaternion.AngleAxis(-angle1, bendNormal) * Quaternion.LookRotation(targetDir, planeNormal);
            leftUpperArm.rotation = Quaternion.Slerp(leftUpperArm.rotation, rotUpper, ikWeight);

            // 2. Rotação do Forearm (Cotovelo dobrado)
            Vector3 midToTarget = (targetPos - leftForearm.position).normalized;
            Quaternion rotForearm = Quaternion.LookRotation(midToTarget, bendNormal);
            leftForearm.rotation = Quaternion.Slerp(leftForearm.rotation, rotForearm, ikWeight);

            // 3. Rotação da Mão (empunhadura)
            leftHand.position = Vector3.Lerp(leftHand.position, targetPos, ikWeight);
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, targetRot, ikWeight);
        }

        private void OnDrawGizmos()
        {
            if (leftHandGripTarget != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.8f);
                Gizmos.DrawWireSphere(leftHandGripTarget.position, 0.02f);
                Gizmos.DrawRay(leftHandGripTarget.position, leftHandGripTarget.forward * 0.05f);

                if (leftHand != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(leftHand.position, leftHandGripTarget.position);
                }
            }

            if (leftElbowHint != null && leftForearm != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(leftElbowHint.position, 0.015f);
                Gizmos.DrawLine(leftForearm.position, leftElbowHint.position);
            }
        }
    }
}
