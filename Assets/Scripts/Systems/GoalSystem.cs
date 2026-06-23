using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GoalSystem : MonoBehaviour
{
    public static GoalSystem Instance { get; private set; }
    
    [Header("Goal")]
    [SerializeField] private int beachTicketCost = 5000;
    
    [Header("UI")]
    [SerializeField] private GameObject goalPanel;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button buyTicketButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TextMeshProUGUI endingText;
    
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    
    const string TicketBoughtKey = "BeachTicketBought";

    void Start()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (buyTicketButton) buyTicketButton.onClick.AddListener(BuyTicket);

        // Persisted across village revisits so the ending can't replay and
        // PawCoins can't be spent on the ticket twice.
        ticketBought = PlayerPrefs.GetInt(TicketBoughtKey, 0) == 1;
    }
    
    public void OpenGoalBoard()
    {
        if (goalPanel) goalPanel.SetActive(true);
        UpdateUI();
    }
    
    public void Close() { if (goalPanel) goalPanel.SetActive(false); }
    
    void UpdateUI()
    {
        int money = GameManager.Instance != null ? GameManager.Instance.PawCoins : 0;
        if (progressText)
        {
            int remaining = Mathf.Max(0, beachTicketCost - money);
            progressText.text = ticketBought
                ? "Beach ticket secured. Lucky Boba Cafe made the dream real."
                : $"Beach Trip Fund\nPawCoins: ${money} / ${beachTicketCost}\nNeeded: ${remaining}";
        }
        if (progressSlider) progressSlider.value = (float)money / beachTicketCost;
        if (buyTicketButton) buyTicketButton.interactable = !ticketBought && money >= beachTicketCost;
    }
    
    private bool ticketBought = false;

    void BuyTicket()
    {
        if (ticketBought) return;
        if (GameManager.Instance != null && GameManager.Instance.SpendMoney(beachTicketCost))
        {
            ticketBought = true;
            PlayerPrefs.SetInt(TicketBoughtKey, 1);
            PlayerPrefs.Save();
            if (buyTicketButton) buyTicketButton.interactable = false;
            StartCoroutine(PlayEnding());
        }
    }
    
    private bool endingInProgress = false;

    IEnumerator PlayEnding()
    {
        endingInProgress = true;
        if (goalPanel) goalPanel.SetActive(false);
        if (endingPanel) endingPanel.SetActive(true);
        
        string[] lines = new[] {
            "After five long days of serving the town...",
            "Every recipe, delivery, and village friendship brought Lucky Boba Cafe back to life.",
            "With enough PawCoins saved, you finally bought the beach ticket.",
            "The cafe is safe, the village is cheering, and tomorrow starts with ocean air.",
            "THE END"
        };
        
        if (endingText)
        {
            endingText.text = "";
            foreach (var line in lines)
            {
                if (!endingInProgress) yield break;
                endingText.text += line + "\n\n";
                yield return new WaitForSecondsRealtime(2f);
            }
        }
        
        yield return new WaitForSecondsRealtime(3f);
        if (endingInProgress)
            SceneManager.LoadScene(SceneNames.MainMenu);
    }

    void OnDisable()
    {
        endingInProgress = false;
    }
}
