using UnityEngine;
using UnityEngine.UI;

public class FollowerProfileSetup : MonoBehaviour
{
    [SerializeField] private Image imageProfile;
    [SerializeField]private Sprite[] m_Sprite;
    private int m_Index = 0;

    private void Awake()
    {
        imageProfile = GetComponent<Image>();
    }

    public void Start()
    {
        SetupImage(1);
    }

    public void SetupImage(int lvl)
    {
        m_Index = lvl - 1;
        imageProfile.sprite = m_Sprite[m_Index];
    }
}
