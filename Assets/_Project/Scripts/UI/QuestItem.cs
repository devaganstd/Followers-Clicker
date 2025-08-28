using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI timeToExpired;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button claimButton;

    [SerializeField] private Quest _quest;
    private GameEngine _engine;

    public void Bind(Quest quest, GameEngine engine)
    {
        _quest = quest;
        _engine = engine;
        /*
                // set expires
                //_quest.expiresAtUnixMs -= 1;
                Debug.Log($"Quest {quest.id} expires in {_quest.expiresAtUnixMs} seconds");
                if (_quest.expiresAtUnixMs <= 0)
                {
                    _quest.status = QuestStatus.Expired;
                }
        */
        UpdateVisual(quest);
    }

    private void UpdateVisual(Quest q)
    {
        titleText.text = $"{_quest.type} Target:{_quest.target}";
        progressText.text = $"{_quest.progress}/{_quest.target}";
        rewardText.text = $"+ {_quest.rewardFollowers} Followers";
        statusText.text = _quest.status.ToString();

        if (_quest.status == QuestStatus.Completed)
        {
            timeToExpired.text = "Done";
            claimButton.gameObject.SetActive(true);
        }
        else if (_quest.status == QuestStatus.Active)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long remain = _quest.expiresAtUnixMs > 0 ? _quest.expiresAtUnixMs - now : 0;
            if (remain < 0) remain = 0;

            // change visual
            if (remain <= 10_000) timeToExpired.color = Color.red;
            else timeToExpired.color = Color.white;

            timeToExpired.text = FormatMMSS(remain);
            claimButton.gameObject.SetActive(q.status == QuestStatus.Completed);
        }
        else if (_quest.status == QuestStatus.Expired)
        {
            timeToExpired.text = "00:00";
            timeToExpired.color = new Color(0.8f,0.4f,0.4f);
            claimButton.gameObject.SetActive(false);
        }

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() => _engine.ClaimQuest(_quest.id));
    }

    public void WireClaim(System.Action onClaim)
    {
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() => onClaim?.Invoke());
    }
    
    private static string FormatMMSS(long remainMs)
    {
        if (remainMs < 0) remainMs = 0;
        long totalSeconds = remainMs / 1000;
        long m = totalSeconds / 60;
        long s = totalSeconds % 60;
        return $"{m:00}:{s:00}";
    }

}
