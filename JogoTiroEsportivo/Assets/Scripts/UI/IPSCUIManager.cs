using UnityEngine;
using UnityEngine.UI;

namespace IPSCSimulator.UI
{
    using IPSCSimulator.Match;
    using IPSCSimulator.Weapons;

    /// <summary>
    /// Gerencia a exibição visual em tela (HUD) do Shot Timer, Splits, Munição e Resultado Final.
    /// Pode ser associado a elementos de Text / TextMeshPro na cena.
    /// </summary>
    public class IPSCUIManager : MonoBehaviour
    {
        [Header("Elementos de Texto HUD")]
        [SerializeField] private Text timerStatusText;
        [SerializeField] private Text currentTimeText;
        [SerializeField] private Text lastSplitText;
        [SerializeField] private Text ammoCounterText;

        [Header("Painel de Resultados (Fim de Pista)")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultSummaryText;

        private void OnEnable()
        {
            Weapon.OnAmmoChanged += UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged += UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered += UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished += ShowFinalScoreSummary;
        }

        private void OnDisable()
        {
            Weapon.OnAmmoChanged -= UpdateAmmoDisplay;
            RangeOfficerTimer.OnTimerStateChanged -= UpdateTimerStateDisplay;
            RangeOfficerTimer.OnShotRegistered -= UpdateShotDisplay;
            RangeOfficerTimer.OnStageFinished -= ShowFinalScoreSummary;
        }

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (timerStatusText != null) timerStatusText.text = "Pressione [T] para Standby";
            if (currentTimeText != null) currentTimeText.text = "00.00s";
            if (lastSplitText != null) lastSplitText.text = "Split: --";
        }

        private void Update()
        {
            if (RangeOfficerTimer.Instance != null && RangeOfficerTimer.Instance.State == TimerState.Running)
            {
                if (currentTimeText != null)
                {
                    currentTimeText.text = $"{RangeOfficerTimer.Instance.CurrentTime:F2}s";
                }
            }
        }

        private void UpdateAmmoDisplay(int current, int max)
        {
            if (ammoCounterText != null)
            {
                ammoCounterText.text = $"{current} / {max}";
            }
        }

        private void UpdateTimerStateDisplay(TimerState state)
        {
            if (timerStatusText == null) return;

            switch (state)
            {
                case TimerState.Idle:
                    timerStatusText.text = "Aguardando início [T]";
                    if (resultPanel != null) resultPanel.SetActive(false);
                    break;
                case TimerState.Standby:
                    timerStatusText.text = "STANDBY...";
                    break;
                case TimerState.Running:
                    timerStatusText.text = "PISTA ABERTA (EM EXECUÇÃO)";
                    break;
                case TimerState.Finished:
                    timerStatusText.text = "PISTA FINALIZADA - [U] para Obrear";
                    break;
            }
        }

        private void UpdateShotDisplay(float totalTime, float split)
        {
            if (lastSplitText != null)
            {
                lastSplitText.text = $"Último Split: {split:F2}s";
            }
        }

        private void ShowFinalScoreSummary(StageScoreResult result)
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultSummaryText != null)
            {
                resultSummaryText.text =
                    $"<b>--- SUMÁRIO IPSC ---</b>\n\n" +
                    $"Tempo Total: {result.TotalTime:F2}s\n" +
                    $"Disparos: {result.ShotsFired}\n" +
                    $"Alpha (5 pts): {result.AlphaCount}\n" +
                    $"Charlie (3 pts): {result.CharlieCount}\n" +
                    $"Delta (1 pt): {result.DeltaCount}\n" +
                    $"Penalidades (Miss / No-Shoot): -{result.Penalties} pts\n" +
                    $"Pontos Líquidos: {result.FinalPoints} pts\n\n" +
                    $"<size=24><b>HIT FACTOR: {result.HitFactor:F4}</b></size>";
            }
        }
    }
}
