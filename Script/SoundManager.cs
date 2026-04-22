using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum BGMName
{
    Title_BGM,
    Lobby_BGM,
    Boss_Normal,
    Boss_Final,
    Stage_Clear,
    Stage_Fail,
}
public enum UISFXName
{
    Click,
    EnhanceSuccess,
    EnhanceFail,
    CraftingSuccess,
    CraftingFail,
}
public enum PlayerSFXName
{
    Move,
    Clean,
    LevelUp,
    Hit,
}

public enum MonsterSFXName
{
    Monster_Attack,
    Monster_Die,
    Monster_Hit,
    Monster_Roar,
}
public enum TowerMoveSFXType
{
    Type1,
    Type2,
    Type3,
}

[System.Serializable]
public struct BGMClipInfo
{
    public BGMName name;
    public AudioClip clip;
}

[System.Serializable]
public struct MonsterSFXClipInfo
{
    public MonsterSFXName name;
    public AudioClip clip;
}

[System.Serializable]
public struct UISFXClipInfo
{
    public UISFXName name;
    public AudioClip clip;
}

[System.Serializable]
public struct PlayerSFXClipInfo
{
    public PlayerSFXName name;
    public AudioClip clip;
}
[System.Serializable]
public struct TowerMoveSFXClipInfo
{
    public TowerMoveSFXType type;
    public AudioClip clip;
}

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance { get => _instance; private set => _instance = value; }

    private const int MASTER_VOLUME_COUNT = 3;
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    [Header("Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("BGM")]
    [SerializeField] private AudioSource _bgmPlayer;
    [SerializeField] private BGMClipInfo[] _bgmClips;

    [Header("Stage BGMs")]
    [SerializeField] private AudioClip[] _stageBgmList;

    [Header("Skill SFX")]
    [SerializeField] private AudioSource _skillSfxPlayer;

    [Header("Player SFX")]
    [SerializeField] private AudioSource _playerActionSfxPlayer; // 이동, 청소용
    [SerializeField] private AudioSource _playerEventSfxPlayer;  // 레벨업, 피격용
    [SerializeField] private PlayerSFXClipInfo[] _playerSfxClips;
    private Coroutine _playerActionCoroutine;
    private Coroutine _playerEventCoroutine;

    [Header("Footstep SFX")]
    [SerializeField, Tooltip("발소리 재생 간격")]
    private float _footstepInterval = 0.5f;
    [SerializeField] private TowerMoveSFXClipInfo[] _towerMoveSfxClips;
    private TowerMoveSFXType _currentTowerMoveType = TowerMoveSFXType.Type1;    // 현재 타워 발소리 타입
    private WaitForSeconds _footstepDelay;   // 발소리 대기 간격

    [Header("Monster SFX")]
    [SerializeField] private AudioSource _monsterSfxPlayer;
    [SerializeField] private MonsterSFXClipInfo[] _monsterSfxClips;

    [Header("UI SFX")]
    [SerializeField] private AudioSource _uiClickPlayer; // 클릭
    [SerializeField] private AudioSource _uiEventPlayer; // 강화, 합성
    [SerializeField] private UISFXClipInfo[] _uiSfxClips;
    private Coroutine _uiClickCoroutine;

    [Header("스킬 동일 클립 최소 재생 간격")]
    [SerializeField] private float _debounceInterval = 0.1f;

    [Header("볼륨 설정 비율")]
    [SerializeField, Range(0f, 1f), Tooltip("3개 이상 겹칠 때 마스터 볼륨")]
    private float _overlapMasterRatio = 0.7f;
    [SerializeField, Range(0f, 1f), Tooltip("로비 효과음 재생 시 배경음 볼륨")]
    private float _lobbyBgmDuckRatio = 0.8f;
    [SerializeField, Range(0f, 1f), Tooltip("인게임 효과음 재생 시 배경음 볼륨")]
    private float _ingameBgmDuckRatio = 0.5f;
    private float _currentDuckRatio = 1.0f; // 현재 씬 배경음 감소 비율 저장

    // 환경설정 볼륨
    private float _uiMasterVolume = 1.0f;
    private float _uiBgmVolume = 1.0f;
    private float _uiSfxVolume = 1.0f;

    public float UIBGMVolume => _uiBgmVolume;
    public float UISFXVolume => _uiSfxVolume;

    // 셔플
    private bool _isPlayingStageBGM = false;
    private List<int> _shuffleOrder = new List<int>();
    private int _shuffleIndex = 0;
    private int _lastStageBGMIndex = -1;

    private Dictionary<BGMName, AudioClip> _bgmDict = new();
    private Dictionary<UISFXName, AudioClip> _uiSfxDict = new();
    private Dictionary<PlayerSFXName, AudioClip> _playerSfxDict = new();
    private Dictionary<MonsterSFXName, AudioClip> _monsterSfxDict = new();
    private Dictionary<TowerMoveSFXType, AudioClip> _towerMoveSfxDict = new();

    // 스킬 관련
    private List<AudioSource> _loopPool = new();                                     // 루프 
    private Dictionary<int, (AudioSource source, int count)> _loopChannels = new();  // 루프 채널   Key : MainTag  /  Value : (AudioSource, 카운트)
    private Dictionary<AudioClip, float> _lastPlayTimes = new();                     // 원샷 마지막 재생 시간

    // 동일 사운드 중첩 방지용
    private Dictionary<AudioClip, float> _clipEndTimes = new();

    private int _activeSFXCount = 0;                     // 현재 재생 중인 효과음 개수

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var info in _bgmClips)          _bgmDict[info.name]          = info.clip;
        foreach (var info in _uiSfxClips)        _uiSfxDict[info.name]        = info.clip;
        foreach (var info in _playerSfxClips)    _playerSfxDict[info.name]    = info.clip;
        foreach (var info in _monsterSfxClips)   _monsterSfxDict[info.name]   = info.clip;
        foreach (var info in _towerMoveSfxClips) _towerMoveSfxDict[info.type] = info.clip;

        LoadVolumes();
    }

    private void Start()
    {
        PlaySceneBGM(SceneManager.GetActiveScene().name);

        _footstepDelay = new WaitForSeconds(_footstepInterval);
    }

    private void Update()
    {
        if (_isPlayingStageBGM      == true
            && _bgmPlayer.isPlaying == false
            && _bgmPlayer.time      == 0f
            && _stageBgmList.Length >  0)
        {
            PlayNextStageBGM();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 모든 효과음 강제로 종료
        StopAllSFX();
        // 씬 전환 시 카운트 초기화
        _activeSFXCount = 0;
        // 씬 BGM
        PlaySceneBGM(scene.name);
        // 중첩 타이머 초기화
        _clipEndTimes.Clear();

        UpdateMixerVolumes();
    }
    private void PlaySceneBGM(string sceneName)
    {
        if (sceneName.Contains("Title"))
        {
            _currentDuckRatio = 1.0f;
            PlayBGM(BGMName.Title_BGM);
        }
        else if (sceneName.Contains("Lobby"))
        {
            _currentDuckRatio = _lobbyBgmDuckRatio;
            PlayBGM(BGMName.Lobby_BGM);
        }
        else
        {
            _currentDuckRatio = _ingameBgmDuckRatio;
            PlayInGameBGM();
        }
    }

    #region BGM

    public void PlayBGM(BGMName bgmName, bool isLoop = true)
    {
        _isPlayingStageBGM = false;

        if (_bgmDict.TryGetValue(bgmName, out AudioClip clip))
        {
            _bgmPlayer.clip = clip;
            _bgmPlayer.loop = isLoop;
            _bgmPlayer.Play();
        }
    }

    public void PlayInGameBGM()
    {
        if (_stageBgmList.Length == 0) return;

        _isPlayingStageBGM = true;
        _bgmPlayer.loop = false; // 끝나면 Update에서 다음 곡

        GenerateShuffleOrder();
        _shuffleIndex = 0;

        PlayStageBGMAt(_shuffleOrder[_shuffleIndex]);
    }

    private void PlayStageBGMAt(int index)
    {
        _bgmPlayer.clip = _stageBgmList[index];
        _bgmPlayer.Play();

        _lastStageBGMIndex = index;
    }

    private void PlayNextStageBGM()
    {
        _shuffleIndex++;

        if (_shuffleIndex >= _shuffleOrder.Count)
        {
            GenerateShuffleOrder();
            _shuffleIndex = 0;
        }

        PlayStageBGMAt(_shuffleOrder[_shuffleIndex]);
    }

    private void GenerateShuffleOrder()
    {
        _shuffleOrder.Clear();
        for (int i = 0; i < _stageBgmList.Length; i++) _shuffleOrder.Add(i);

        // 셔플
        for (int i = _shuffleOrder.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (_shuffleOrder[i], _shuffleOrder[rand]) = (_shuffleOrder[rand], _shuffleOrder[i]);
        }

        // 방금 재생된 브금 제외
        if (_lastStageBGMIndex != -1 && _shuffleOrder.Count > 1)
        {
            if (_shuffleOrder[0] == _lastStageBGMIndex)
            {
                (_shuffleOrder[0], _shuffleOrder[1]) = (_shuffleOrder[1], _shuffleOrder[0]);
            }
        }
    }

    public void StopBGM()
    {
        _isPlayingStageBGM = false;
        _bgmPlayer.Stop();
    }
    #endregion

    #region UI, 플레이어, 몬스터
    private bool CanPlayClip(AudioClip clip)
    {
        if (clip == null) return false;

        float currentTime = Time.time;
        if (_clipEndTimes.TryGetValue(clip, out float endTime) && currentTime < endTime)
        {
            return false; // 아직 재생 중이므로 생략
        }

        _clipEndTimes[clip] = currentTime + clip.length;
        return true;
    }

    public void PlayMonsterSFX(MonsterSFXName sfxName, float volume = 1f)
    {
        if (_monsterSfxDict.TryGetValue(sfxName, out AudioClip clip))
        {
            if (CanPlayClip(clip) == false) return;

            _monsterSfxPlayer.PlayOneShot(clip, volume);
            StartCoroutine(TrackSFXDuration(clip.length));
        }
    }

    public void PlayUISFX(UISFXName sfxName, float volume = 1f)
    {
        if (_uiSfxDict.TryGetValue(sfxName, out AudioClip clip))
        {
            // 클릭음 (이전 클릭음 덮어쓰기)
            if (sfxName == UISFXName.Click)
            {
                // 재생 중지
                StopTrackedCoroutine(ref _uiClickCoroutine);

                _uiClickPlayer.clip = clip;
                _uiClickPlayer.volume = volume;
                _uiClickPlayer.Play();

                // 재생 후 콜백으로 코루틴 비우기
                _uiClickCoroutine = StartCoroutine(TrackAndClearSFX(clip.length, () => _uiClickCoroutine = null));
            }
            // 강화, 합성 (재생 중이면 무시하고 원샷)
            else
            {
                if (CanPlayClip(clip) == false) return;

                _uiEventPlayer.PlayOneShot(clip, volume);
                StartCoroutine(TrackSFXDuration(clip.length));
            }
        }
    }

    public void PlayPlayerSFX(PlayerSFXName sfxName, float volume = 1f)
    {
        switch (sfxName)
        {
            case PlayerSFXName.Move:
                PlayMoveLoopSFX(volume);
                break;

            case PlayerSFXName.Clean:
                if (_playerSfxDict.TryGetValue(sfxName, out AudioClip cleanClip))
                    PlayActionSFX(cleanClip, volume);
                break;

            case PlayerSFXName.LevelUp:
            case PlayerSFXName.Hit:
                if (_playerSfxDict.TryGetValue(sfxName, out AudioClip eventClip))
                    PlayEventSFX(eventClip, volume);
                break;
        }
    }

    // 걷기
    private void PlayMoveLoopSFX(float volume)
    {
        if (!_towerMoveSfxDict.TryGetValue(_currentTowerMoveType, out AudioClip clip)) return;

        // 이미 걷는 소리가 재생 중이면 무시
        if (_playerActionCoroutine != null && _playerActionSfxPlayer.clip == clip) return;

        StopTrackedCoroutine(ref _playerActionCoroutine);
        _playerActionCoroutine = StartCoroutine(TrackFootstepLoop(clip, volume));
    }

    // 청소
    private void PlayActionSFX(AudioClip clip, float volume)
    {
        StopTrackedCoroutine(ref _playerActionCoroutine);

        _playerActionSfxPlayer.clip = clip;
        _playerActionSfxPlayer.volume = volume;
        _playerActionSfxPlayer.Play();

        _playerActionCoroutine = StartCoroutine(TrackAndClearSFX(clip.length, () => _playerActionCoroutine = null));
    }

    // 피격, 레벨업
    private void PlayEventSFX(AudioClip clip, float volume)
    {
        StopTrackedCoroutine(ref _playerEventCoroutine);

        _playerEventSfxPlayer.clip = clip;
        _playerEventSfxPlayer.volume = volume;
        _playerEventSfxPlayer.Play();

        _playerEventCoroutine = StartCoroutine(TrackAndClearSFX(clip.length, () => _playerEventCoroutine = null));
    }

    // 중지
    public void StopPlayerSFX()
    {
        _playerActionSfxPlayer.Stop();
        StopTrackedCoroutine(ref _playerActionCoroutine);
    }

    // Player, UI SFX 중지
    private void StopTrackedCoroutine(ref Coroutine tracker)
    {
        if (tracker != null)
        {
            StopCoroutine(tracker);
            tracker = null;
            _activeSFXCount = Mathf.Max(0, _activeSFXCount - 1);
            UpdateMixerVolumes();
        }
    }

    // Player, UI SFX 재생 추적
    private IEnumerator TrackAndClearSFX(float duration, System.Action onComplete)
    {
        _activeSFXCount++;
        UpdateMixerVolumes();

        yield return new WaitForSeconds(duration);

        _activeSFXCount = Mathf.Max(0, _activeSFXCount - 1);
        UpdateMixerVolumes();

        // 코루틴 비우기
        onComplete?.Invoke();
    }

    // 발소리 전용
    private IEnumerator TrackFootstepLoop(AudioClip clip, float volume)
    {
        _activeSFXCount++;
        UpdateMixerVolumes();

        while (true)
        {
            _playerActionSfxPlayer.clip = clip;
            _playerActionSfxPlayer.volume = volume;
            _playerActionSfxPlayer.Play();

            // 인스펙터에서 설정한 간격만큼 대기
            yield return _footstepDelay;
        }
    }
    #endregion

    #region 스킬

    public void PlaySkillActiveSFX(SkillSoundData data)
    {
        if (data == null) return;
        PlayRandomSkillClip(data.onCast);
    }

    public void PlaySkillExpireSFX(SkillSoundData data)
    {
        if (data == null) return;
        PlayRandomSkillClip(data.onExpire);
    }

    public void PlaySkillSpecialSFX(SkillSoundData data)
    {
        if (data == null) return;
        PlayRandomSkillClip(data.onSpecial);
    }

    public void PlayRandomSkillClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySkillClip(clips[Random.Range(0, clips.Length)]);
    }

    public void NotifySkillSFX(float clipDuration)
    {
        StartCoroutine(TrackSFXDuration(clipDuration));
    }

    public void PlaySkillClip(AudioClip clip)
    {
        if (clip == null) return;

        float time = Time.time;
        if (_lastPlayTimes.TryGetValue(clip, out float last) && time - last < _debounceInterval) return;

        _lastPlayTimes[clip] = time;
        _skillSfxPlayer.PlayOneShot(clip);
        StartCoroutine(TrackSFXDuration(clip.length));
    }

    public void PlaySkillLoopSFX(int mainTag, SkillSoundData data)
    {
        if (data == null || data.loopClip == null) return;
        if (_loopChannels.TryGetValue(mainTag, out var entry))
        {
            _loopChannels[mainTag] = (entry.source, entry.count + 1);
            return;
        }

        AudioSource slot = FindFreeSlot();
        if (slot == null)
        {
            slot = _skillSfxPlayer.gameObject.AddComponent<AudioSource>();
            slot.playOnAwake = false;
            slot.loop = true;
            slot.outputAudioMixerGroup = _skillSfxPlayer.outputAudioMixerGroup;
            _loopPool.Add(slot);
        }

        slot.clip = data.loopClip;
        slot.volume = 1f;
        slot.Play();
        _loopChannels[mainTag] = (slot, 1);

        _activeSFXCount++;
        UpdateMixerVolumes();
    }

    public void StopSkillLoopSFX(int mainTag)
    {
        if (!_loopChannels.TryGetValue(mainTag, out var entry)) return;
        int newCount = entry.count - 1;

        if (newCount <= 0)
        {
            if (entry.source != null) { entry.source.Stop(); entry.source.clip = null; }
            _loopChannels.Remove(mainTag);

            _activeSFXCount = Mathf.Max(0, _activeSFXCount - 1);
            UpdateMixerVolumes();
        }
        else _loopChannels[mainTag] = (entry.source, newCount);
    }

    private AudioSource FindFreeSlot()
    {
        foreach (var source in _loopPool) if (!source.isPlaying) return source;
        return null;
    }

    private void ClearAllSkillLoopSFX()
    {
        foreach (var entry in _loopChannels.Values) if (entry.source != null) entry.source.Stop();
        _loopChannels.Clear();
        foreach (var source in _loopPool) if (source != null) Destroy(source);
        _loopPool.Clear();
    }
    #endregion

    #region 오디오 믹서 제어, 볼륨 저장

    public void LoadVolumes()
    {
        _uiBgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_PARAM, 1.0f);
        _uiSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_PARAM, 1.0f);
        UpdateMixerVolumes();
    }

    private void UpdateMixerVolumes()
    {
        if (_audioMixer == null) return;

        // 3개 이상 겹칠 시 마스터 볼륨 감소
        float finalMaster = _uiMasterVolume;
        if (_activeSFXCount >= MASTER_VOLUME_COUNT) finalMaster *= _overlapMasterRatio;
        _audioMixer.SetFloat(MASTER_VOLUME_PARAM, ConvertToDb(finalMaster));

        // 효과음 재생 시 씬에 맞게 BGM 오토 더킹 적용
        float finalBgm = _uiBgmVolume;
        if (_activeSFXCount > 0) finalBgm *= _currentDuckRatio;
        _audioMixer.SetFloat(BGM_VOLUME_PARAM, ConvertToDb(finalBgm));

        // SFX는 UI 설정값 그대로
        _audioMixer.SetFloat(SFX_VOLUME_PARAM, ConvertToDb(_uiSfxVolume));
    }

    private float ConvertToDb(float linear) => linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

    private IEnumerator TrackSFXDuration(float duration)
    {
        _activeSFXCount++;
        UpdateMixerVolumes();

        yield return new WaitForSeconds(duration);

        _activeSFXCount = Mathf.Max(0, _activeSFXCount - 1);
        UpdateMixerVolumes();
    }

    public void SetBGMVolume(float vol)
    {
        _uiBgmVolume = Mathf.Clamp01(vol);
        UpdateMixerVolumes();
    }
    public void SetSFXVolume(float vol)
    {
        _uiSfxVolume = Mathf.Clamp01(vol);
        UpdateMixerVolumes();
        
    }
    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_PARAM, _uiBgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_PARAM, _uiSfxVolume);
        UpdateMixerVolumes();
        
    }

    #endregion


    // 스테이지 결과용
    public void PlayStageResult(BGMName bgmName)
    {
        PlayBGM(bgmName, false);
        StopAllSFX();
    }


    // 모든 SFX 강제 종료
    public void StopAllSFX()
    {
        // 플레이어
        if (_playerActionSfxPlayer != null) _playerActionSfxPlayer.Stop();
        if (_playerEventSfxPlayer != null) _playerEventSfxPlayer.Stop();
        StopTrackedCoroutine(ref _playerActionCoroutine);
        StopTrackedCoroutine(ref _playerEventCoroutine);

        // UI
        if (_uiClickPlayer != null) _uiClickPlayer.Stop();
        if (_uiEventPlayer != null) _uiEventPlayer.Stop();
        StopTrackedCoroutine(ref _uiClickCoroutine);

        // 몬스터
        if (_monsterSfxPlayer != null) _monsterSfxPlayer.Stop();

        // 스킬
        if (_skillSfxPlayer != null) _skillSfxPlayer.Stop();
        // 스킬 루프 사운드
        ClearAllSkillLoopSFX();
    }

    // 발소리 설정
    public void SetTowerMoveSFX(TowerMoveSFXType type)
        => _currentTowerMoveType = type;

}