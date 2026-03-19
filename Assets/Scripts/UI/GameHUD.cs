using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Top bar")]
    public TextMeshProUGUI scoreValueText;
    public TextMeshProUGUI levelNameText;
    public Button pauseButton;

    [Header("Color pills")]
    public GameObject colorPillPrefab;
    public Transform colorPillParent;

    [Header("Pause panel")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button pauseQuitButton;

    [Header("Win panel")]
    public GameObject winPanel;
    public TextMeshProUGUI winScoreText;
    public Button nextLevelButton;
    public Button winRestartButton;
    public Button winQuitButton;

    private GameObject[] pills = new GameObject[5];
    private TextMeshProUGUI[] pillTexts = new TextMeshProUGUI[5];

    void Start()
    {
        pausePanel.SetActive(false);
        winPanel.SetActive(false);

        LevelManager lm = LevelManager.Instance;
        if (lm != null)
        {
            levelNameText.text = lm.levelData != null ? lm.levelData.levelName : "Level 1";
            lm.onScoreChanged.AddListener(UpdateScore);
            lm.onColorProgress.AddListener(UpdateColorPill);
            lm.onLevelComplete.AddListener(ShowWin);
            BuildColorPills(lm);
        }

        pauseButton.onClick.AddListener(OpenPause);
        resumeButton.onClick.AddListener(ClosePause);
        restartButton.onClick.AddListener(Restart);
        nextLevelButton.onClick.AddListener(LoadNextLevel);
        if (pauseQuitButton != null) pauseQuitButton.onClick.AddListener(QuitToMenu);
        if (winRestartButton != null) winRestartButton.onClick.AddListener(Restart);
        if (winQuitButton != null) winQuitButton.onClick.AddListener(QuitToMenu);
        scoreValueText.text = "0";
    }

    void BuildColorPills(LevelManager lm)
    {
        for (int i = 0; i < 5; i++)
        {
            int req = lm.GetRequirement(i);
            var pill = Instantiate(colorPillPrefab, colorPillParent);
            pills[i] = pill;

            var swatch = pill.GetComponentInChildren<Image>();
            swatch.color = JellyTile.JellyColors[i];

            pillTexts[i] = pill.GetComponentInChildren<TextMeshProUGUI>();
            pillTexts[i].text = req.ToString();

            pill.SetActive(req > 0);
        }
    }

    void UpdateScore(int newScore)
    {
        scoreValueText.text = newScore.ToString("N0");
    }

    void UpdateColorPill(int colorIndex, int remaining)
    {
        if (colorIndex < 0 || colorIndex >= 5 || pills[colorIndex] == null) return;
        pillTexts[colorIndex].text = remaining.ToString();

        var group = pills[colorIndex].GetComponent<CanvasGroup>();
        if (group == null) group = pills[colorIndex].AddComponent<CanvasGroup>();
        group.alpha = remaining <= 0 ? 0.35f : 1f;
    }

    void OpenPause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void ClosePause()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void ShowWin()
    {
        winPanel.SetActive(true);
        if (int.TryParse(scoreValueText.text.Replace(",", ""), out int s))
            winScoreText.text = s.ToString("N0");
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SceneManager.LoadScene("LevelSelection");
    }

    void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelection");
    }

    void OnDestroy() { Time.timeScale = 1f; }
}