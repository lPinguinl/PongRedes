using UnityEngine;
using TMPro;

public class ScoreBoardUI : MonoBehaviour
{
    [Header("Marcador")]
    [SerializeField] private TMP_Text team0ScoreLabel;
    [SerializeField] private TMP_Text team1ScoreLabel;

    [Header("Panel de Pausa")]
    [SerializeField] private CanvasGroup pauseCanvas;
    [SerializeField] private TMP_Text pauseMessageLabel;

    [Header("Panel Final")]
    [SerializeField] private CanvasGroup endCanvas;
    [SerializeField] private TMP_Text endMessageLabel;

    private void Awake()
    {
        ShowPause(false, string.Empty);
        ShowEnd(false, string.Empty);
    }

    public void UpdateScore(int team0Score, int team1Score)
    {
        team0ScoreLabel.text = team0Score.ToString();
        team1ScoreLabel.text = team1Score.ToString();
    }

    public void ShowPause(bool visible, string message)
    {
        pauseCanvas.alpha = visible ? 1f : 0f;
        pauseCanvas.interactable = visible;
        pauseCanvas.blocksRaycasts = visible;
        pauseMessageLabel.text = message;
    }

    public void ShowEnd(bool visible, string message)
    {
        endCanvas.alpha = visible ? 1f : 0f;
        endCanvas.interactable = visible;
        endCanvas.blocksRaycasts = visible;
        endMessageLabel.text = message;
    }
}