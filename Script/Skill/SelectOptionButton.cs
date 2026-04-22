using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SelectOptionButton : MonoBehaviour
{
    [Header("UI 연결")]
    public Image IconImage;             // 아이콘 이미지
    public TextMeshProUGUI NameText;    // 스킬 이름 텍스트
    public TextMeshProUGUI DescText;    // 스킬 설명 텍스트
    private Button _button;             // 버튼

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void Init(UnityAction onClickAction)
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(onClickAction);
    }

    // 버튼 컨텐츠 설정
    public void Setup(string name, string desc, Sprite icon)
    {
        NameText.text = name;
        DescText.text = desc;

        if (IconImage != null && icon != null)
            IconImage.sprite = icon;
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveAllListeners();
    }
}
