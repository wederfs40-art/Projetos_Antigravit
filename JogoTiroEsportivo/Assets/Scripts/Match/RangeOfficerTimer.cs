using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IPSCSimulator.Match
{
    using IPSCSimulator.Targets;
    using IPSCSimulator.Weapons;

    public enum TimerState
    {
        Idle,
        Standby,
        Running,
        Finished
    }

    /// <summary>
    /// Estrutura para os resultados consolidados da pista.
    /// </summary>
    public struct StageScoreResult
    {
        public float TotalTime;
        public int ShotsFired;
        public int AlphaCount;
        public int CharlieCount;
        public int DeltaCount;
        public int NoShootCount;
        public int MissCount;
        public int RawScore;
        public int Penalties;
        public int FinalPoints;
        public float HitFactor;
    }

    /// <summary>
    /// Cronômetro oficial da pista (Shot Timer) e Juiz de Pista (Range Officer).
    /// </summary>
    public class RangeOfficerTimer : MonoBehaviour
    {
        public static RangeOfficerTimer Instance { get; private set; }

        [Header("Áudio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip beepSound;

        [Header("Configuração de Regras IPSC")]
        [Tooltip("Mínimo de tiros pontuáveis exigidos por alvo de papel (padrão IPSC: 2).")]
        [SerializeField] private int requiredShotsPerTarget = 2;
        [SerializeField] private List<IPSCTarget> stageTargets = new List<IPSCTarget>();

        // Estados e Tempos
        private TimerState _state = TimerState.Idle;
        private float _stageTime = 0f;
        private float _lastShotTime = 0f;
        private readonly List<float> _splits = new List<float>();

        // Pontuação
        private int _totalAlpha = 0;
        private int _totalCharlie = 0;
        private int _totalDelta = 0;
        private int _totalNoShoot = 0;
        private int _totalMiss = 0;

        // Eventos para UI e outros sistemas
        public static event Action<TimerState> OnTimerStateChanged;
        public static event Action<float, float> OnShotRegistered; // (tempoAtual, split)
        public static event Action<StageScoreResult> OnStageFinished;

        public TimerState State => _state;
        public float CurrentTime => _stageTime;
        public IReadOnlyList<float> Splits => _splits;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            Weapon.OnWeaponFired += HandleShotFired;
        }

        private void OnDisable()
        {
            Weapon.OnWeaponFired -= HandleShotFired;
        }

        private void Update()
        {
            if (_state == TimerState.Running)
            {
                _stageTime += Time.deltaTime;
            }

            // Tecla T: Inicia contagem
            if (Input.GetKeyDown(KeyCode.T) && _state == TimerState.Idle)
            {
                StartStageSequence();
            }

            // Tecla Y: Finaliza contagem
            if (Input.GetKeyDown(KeyCode.Y) && _state == TimerState.Running)
            {
                FinishStage();
            }

            // Tecla U: Reset e Obrear
            if (Input.GetKeyDown(KeyCode.U) && _state == TimerState.Finished)
            {
                ResetStageAndObrear();
            }
        }

        /// <summary>
        /// Inicia a sequência de partida do Range Officer ("Standby" -> Delay 1 a 4s -> Beep).
        /// </summary>
        public void StartStageSequence()
        {
            if (_state != TimerState.Idle) return;
            StartCoroutine(StartStageRoutine());
        }

        private IEnumerator StartStageRoutine()
        {
            _state = TimerState.Standby;
            OnTimerStateChanged?.Invoke(_state);
            ResetTimerData();

            Debug.Log("[RO] Are you ready? ... Standby!");

            // Delay regulamentar aleatório IPSC (1 a 4 segundos)
            float randomDelay = UnityEngine.Random.Range(1.0f, 4.0f);
            yield return new WaitForSeconds(randomDelay);

            // Beep do Shot Timer
            if (audioSource != null && beepSound != null)
            {
                audioSource.PlayOneShot(beepSound);
            }

            _state = TimerState.Running;
            _stageTime = 0f;
            _lastShotTime = 0f;
            OnTimerStateChanged?.Invoke(_state);
            Debug.Log("[RO] BEEP! Cronômetro iniciado!");
        }

        private void HandleShotFired()
        {
            if (_state != TimerState.Running) return;

            float currentShotTime = _stageTime;
            float split = (_splits.Count == 0) ? currentShotTime : (currentShotTime - _lastShotTime);
            _lastShotTime = currentShotTime;
            _splits.Add(split);

            OnShotRegistered?.Invoke(currentShotTime, split);
            Debug.Log($"[SHOT #{_splits.Count}] Tempo: {currentShotTime:F2}s | Split: {split:F2}s");
        }

        public void RegisterTargetHit(IPSCTarget target, IPSCZoneType zone)
        {
            if (_state != TimerState.Running) return;

            switch (zone)
            {
                case IPSCZoneType.Alpha: _totalAlpha++; break;
                case IPSCZoneType.Charlie: _totalCharlie++; break;
                case IPSCZoneType.Delta: _totalDelta++; break;
                case IPSCZoneType.NoShoot: _totalNoShoot++; break;
            }
        }

        /// <summary>
        /// Encerra a pista, processa as penalidades de Miss e calcula o Hit Factor.
        /// </summary>
        public void FinishStage()
        {
            if (_state != TimerState.Running) return;

            _state = TimerState.Finished;

            // Calcula Misses nos alvos não atingidos com a quantidade mínima necessária
            _totalMiss = 0;
            foreach (IPSCTarget target in stageTargets)
            {
                if (target != null && !target.IsNoShoot)
                {
                    int hitsOnTarget = target.Hits.Count;
                    if (hitsOnTarget < requiredShotsPerTarget)
                    {
                        _totalMiss += (requiredShotsPerTarget - hitsOnTarget);
                    }
                }
            }

            // Pontuação: A = 5, C = 3, D = 1, Miss = -10, NoShoot = -10
            int rawScore = (_totalAlpha * 5) + (_totalCharlie * 3) + (_totalDelta * 1);
            int penalties = (_totalMiss * 10) + (_totalNoShoot * 10);
            int finalScore = Mathf.Max(0, rawScore - penalties);

            // Hit Factor = Pontos Líquidos / Tempo Total
            float hitFactor = (_stageTime > 0.001f) ? ((float)finalScore / _stageTime) : 0f;

            StageScoreResult result = new StageScoreResult
            {
                TotalTime = _stageTime,
                ShotsFired = _splits.Count,
                AlphaCount = _totalAlpha,
                CharlieCount = _totalCharlie,
                DeltaCount = _totalDelta,
                NoShootCount = _totalNoShoot,
                MissCount = _totalMiss,
                RawScore = rawScore,
                Penalties = penalties,
                FinalPoints = finalScore,
                HitFactor = hitFactor
            };

            OnTimerStateChanged?.Invoke(_state);
            OnStageFinished?.Invoke(result);

            Debug.Log("==================================================");
            Debug.Log("               SUMÁRIO DA PISTA (IPSC)            ");
            Debug.Log("==================================================");
            Debug.Log($"Tempo Total: {_stageTime:F2} segundos");
            Debug.Log($"Total de Disparos: {_splits.Count}");
            Debug.Log($"Zonas: {_totalAlpha} Alpha | {_totalCharlie} Charlie | {_totalDelta} Delta");
            Debug.Log($"Penalidades: {_totalMiss} Misses | {_totalNoShoot} No-Shoots (-{penalties} pts)");
            Debug.Log($"Pontos Líquidos: {finalScore} pts");
            Debug.Log($"HIT FACTOR FINAL: {hitFactor:F4}");
            Debug.Log("==================================================");
        }

        public void ResetStageAndObrear()
        {
            ResetTimerData();
            foreach (var target in stageTargets)
            {
                if (target != null) target.ObrearAlvo();
            }
            _state = TimerState.Idle;
            OnTimerStateChanged?.Invoke(_state);
        }

        private void ResetTimerData()
        {
            _stageTime = 0f;
            _lastShotTime = 0f;
            _splits.Clear();
            _totalAlpha = 0;
            _totalCharlie = 0;
            _totalDelta = 0;
            _totalNoShoot = 0;
            _totalMiss = 0;
        }
    }
}
