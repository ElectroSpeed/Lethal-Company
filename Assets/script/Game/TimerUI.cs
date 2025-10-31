using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private GameLoop _gameLoop;

    private void OnEnable()
    {
        if (_gameLoop != null)
        {
            GameLoop.OnElapsedTimeChanged += UpdateTimerText;
        }
    }

    private void OnDisable()
    {
        if (_gameLoop != null)
        {
            GameLoop.OnElapsedTimeChanged -= UpdateTimerText;
        }
    }

    private void UpdateTimerText(float newValue)
    {
        float timeRemaining = Mathf.Max(0f, newValue);
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        _timerText.text = $"{minutes:00}:{seconds:00}";
    }
}