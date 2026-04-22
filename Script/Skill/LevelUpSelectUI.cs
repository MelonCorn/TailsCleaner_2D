using UnityEngine;
using UnityEngine.UI;

public class LevelUpSelectUI : MonoBehaviour
{
    [Header("보유 스킬 아이콘")]
    [SerializeField] Image[] ActiveSkillIcons;
    [SerializeField] Image[] PassiveSkillIcons;

    [Header("선택지 버튼")]
    public SelectOptionButton[] OptionButtons;

    [Header("레벨업 이벤트 채널")]
    [SerializeField] private IntEventChannelSO _onLevelUp;

    [Header("체력 회복 선택지 설정")]
    [SerializeField] string _healName = "체력 회복";
    [SerializeField] string _healDesc = "최대 체력의 30%를 회복한다";
    [SerializeField] Sprite _healIcon;

    [Header("화면 모드")]
    [SerializeField] bool _isVertical;

    private LevelUpSelect _levelUpSelect;

    private void Start()
    {
        _levelUpSelect = SkillManager.Instance.GetComponent<LevelUpSelect>();

        // 선택지 버튼 이벤트 설정
        for (int i = 0; i < OptionButtons.Length; i++)
        { 
            // 클로저 이슈 방지
            int captureIndex = i;

            OptionButtons[i].Init(() => OnOptionClick(captureIndex));
        }

        // 레벨업 채널 구독
        _onLevelUp.AddListener(LevelUp);

        // 설정 다 하고 끄기
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _onLevelUp.RemoveListener(LevelUp);
    }

    // 레벨 업 시 호출
    public void LevelUp(int level)
    {
        if (StageController.Instance != null)
        {
            if (StageController.Instance.IsBossIntroPlaying ||
                StageController.Instance.IsGameplayBlocked)
            {
                return;
            }
        }
         
        if (_isVertical != UIManager.Instance.IsVertical) return;

        // 새로운 선택지 생성
        _levelUpSelect.GenerateOptions(level);

        // 선택지 전부 회복인지 체크
        if (IsAllHealOptions() == true)
        {
            // 다 회복이면 그냥 회복 바로
            _levelUpSelect.SelectOption(0);
            return;
        }

        // UI On
        gameObject.SetActive(true);

        // 상단 보유 스킬 아이콘 갱신
        UpdateSkillIcons();

        // 선택지 버튼 갱신
        UpdateOptionButtons();

        if (StageController.Instance != null)
            StageController.Instance.NotifySkillSelectOpened();

        // 게임 일시정지
        Time.timeScale = 0f;
    }
    
    
    // 모든 선택지 체력 회복인지 체크
    private bool IsAllHealOptions()
    {
        // 선택지가 아예 없으면 false
        if (_levelUpSelect.CurrentOptions.Count == 0) return false;

        // 선택지 순회
        foreach (var option in _levelUpSelect.CurrentOptions)
        {
            // 하나라도 회복이 아니면 즉시 false
            if (option.Type != LevelUpSelect.OPTION_TYPE.Heal)
                return false;
        }

        // 전부 회복임
        return true;
    }


    // 상단 보유 스킬 아이콘 갱신
    private void UpdateSkillIcons()
    {
        // 보유 액티브 스킬
        var activeSkills = SkillManager.Instance.MyActiveSkills;

        for (int i = 0; i < ActiveSkillIcons.Length; i++)
        {
            if (i < activeSkills.Count)
            {
                // 아이콘 설정
                ActiveSkillIcons[i].sprite = SkillDataLoader.GetActiveSkillData(activeSkills[i].MainTag).Icon;
                // 하얗게
                ActiveSkillIcons[i].color = Color.white;
            }
            else
            {
                // 빈 칸
                ActiveSkillIcons[i].color = Color.clear;
            }
        }

        // 보유 패시브 스킬
        var passiveSkills = SkillManager.Instance.MyPassiveSkills;
        for (int i = 0; i < PassiveSkillIcons.Length; i++)
        {
            if (i < passiveSkills.Count)
            {
                PassiveSkillIcons[i].sprite = passiveSkills[i].Icon;
                PassiveSkillIcons[i].color = Color.white;
            }
            else
            {
                PassiveSkillIcons[i].color = Color.clear;
            }
        }
    }

    // 선택지 버튼 갱신
    private void UpdateOptionButtons()
    {
        // 현재 선택지
        var options = _levelUpSelect.CurrentOptions;

        for (int i = 0; i < OptionButtons.Length; i++)
        {
            OptionButtons[i].gameObject.SetActive(true);
            var optionData = options[i];

            string name = "";
            string desc = "";
            Sprite icon = null;

            // 타입에 맞게
            switch (optionData.Type)
            {
                case LevelUpSelect.OPTION_TYPE.Active:      // 액티브
                    ActiveSkillData activeSkill = SkillDataLoader.GetActiveSkillData(optionData.TargetMainTag);
                    name = SkillDataLoader.GetString(activeSkill.NameStringId);
                    desc = SkillDataLoader.GetString(optionData.ActiveData.DescStringId);;
                    icon = activeSkill.Icon;
                    break;

                case LevelUpSelect.OPTION_TYPE.Passive:     // 패시브
                    name = SkillDataLoader.GetString(optionData.PassiveData.NameStringId);
                    desc = SkillDataLoader.GetString(optionData.PassiveData.DescStringId);
                    icon = optionData.PassiveData.Icon;
                    break;

                case LevelUpSelect.OPTION_TYPE.Heal:        // 회복
                    name = _healName;
                    desc = _healDesc;
                    icon = _healIcon;
                    break;
            }

            // 선택지 내용 설정
            OptionButtons[i].Setup(name, desc, icon);
        }
    }

    // 버튼 클릭 시
    private void OnOptionClick(int index)
    {
        // 스킬 적용
        _levelUpSelect.SelectOption(index);

        // UI 닫고 게임 재개
        CloseUI();
    }

    private void CloseUI()
    {
        // ui 끄고
        gameObject.SetActive(false);

        if(StageController.Instance != null)
            StageController.Instance.NotifySkillSelectClosed();

        // 게임 다시 시작
        Time.timeScale = 1f;
    }
}
