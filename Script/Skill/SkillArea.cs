using System.Collections.Generic;
using UnityEngine;

public class SkillArea<TModifierData> : SkillObjectBase
    where TModifierData : class, new()
{
    protected TModifierData _modifierData;

    // 장판 범위 내 적
    protected HashSet<MonsterBase> _monstersInArea = new();
    
    // 캐싱 버퍼
    private List<MonsterBase> _tickBuffer = new List<MonsterBase>(32);

    // 최근 틱 피해 시간
    protected float _lastTickTime;
    
    // 장판 위 플레이어 상태
    protected bool _isPlayerInArea = false;

    // 장판형은 초기화 시 방향이 대체로 없음
    // 근데 속도가 추가되면서 유도될 수 있음
    public virtual void Init(ActiveSkill owner, TModifierData modifierData, Vector2 dir = default)
    {
        _monstersInArea.Clear();

        _lastTickTime = Time.time;

        _modifierData = modifierData;

        base.Init(owner, dir);
    }

    protected override void Update()
    {
        // 틱 주기 없으면 무시
        if (_runtimeFinalStat.TickRate <= 0f) return;

        // 틱 주기마다
        if (Time.time >= _lastTickTime + _runtimeFinalStat.TickRate)
        {
            // 틱 처리
            OnTick();
            ProcessTick();
            _lastTickTime = Time.time;
        }

        base.Update();  // 수명 체크, 스노우볼링
    }


    // 충돌 감지
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Monster"))          // 몬스터
        {
            if (col.TryGetComponent(out MonsterBase monster))
            {
                _monstersInArea.Add(monster);
                OnMonsterEnter(monster);
            }
        }
        else if (col.CompareTag("Player"))      // 플레이어
        {
            _isPlayerInArea = true;
            OnPlayerEnter();
        }
        else if (col.CompareTag("MonsterBullet")) // 적 투사체
        {
            if (col.TryGetComponent(out PoolObject projectile))
            {
                OnBulletEnter(projectile);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Monster"))          // 몬스터
        {
            if (col.TryGetComponent(out MonsterBase monster))
            {
                if (_monstersInArea.Remove(monster))
                    OnMonsterExit(monster);
            }
        }
        else if (col.CompareTag("Player"))      // 플레이어
        {
            if (_isPlayerInArea)
            {
                _isPlayerInArea = false;
                OnPlayerExit();
            }
        }
        else if (col.CompareTag("MonsterBullet")) // 적 투사체
        {
            if (col.TryGetComponent(out MonsterProjectile projectile))
            {
                OnBulletExit(projectile);
            }
        }
    }

    protected virtual void OnMonsterEnter(MonsterBase monster) { }
    protected virtual void OnMonsterExit(MonsterBase monster) { }
    protected virtual void OnPlayerEnter() { }
    protected virtual void OnPlayerExit() { }
    protected virtual void OnBulletEnter(PoolObject projectile) { }
    protected virtual void OnBulletExit(MonsterProjectile projectile) { }

    // 틱 처리
    // 범위 내 적에게 피해
    // 자식의 틱 로직, 패시브 틱 로직 실행
    private void ProcessTick()
    {
        // 범위 내 적이 null인 경우 삭제
        _monstersInArea.RemoveWhere(m => m == null || m.gameObject.activeInHierarchy == false);

        // 버퍼 초기화
        _tickBuffer.Clear();

        // 범위 내 몬스터들 버퍼에 복사
        foreach (var monster in _monstersInArea)
        {
            _tickBuffer.Add(monster);
        }

        // 버퍼 순회
        for (int i = 0; i < _tickBuffer.Count; i++)
        {
            var monster = _tickBuffer[i];

            // 원본에서 죽은 애들 청소
            if (monster == null)
            {
                _monstersInArea.Remove(monster);
                continue;
            }

            // 데미지 처리 
            monster.TakeDamage(GetFinalDamage());

            // 전용 모디파이어 로직
            OnTick(monster);

            // 넉백 시도
            TryKnockback(monster);
        }
    }

    // 틱마다 장판의 적에게 호출
    // 비누 거품의 빨래당함의 서브태그 - 집중공략 
    // 약화된 적이라면 틱마다 최대 체력 감소
    protected virtual void OnTick(MonsterBase monster) { }

    // 그냥 틱마다 호출
    protected virtual void OnTick() { }

    // 소멸 시 범위 내 적 정리
    protected override void OnExpire()
    {
        // 플레이어가 장판 위에 있으면 
        if (_isPlayerInArea == true)
        {
            // 나가기 처리 (버프 빼기)
            OnPlayerExit();
            _isPlayerInArea = false;
        }

        // 몬스터들도 마찬가지
        foreach (var monster in _monstersInArea)
        {
            if (monster != null && monster.gameObject.activeInHierarchy)
            {
                Debug.Log("몬스터 장판 나감");
                OnMonsterExit(monster);
            }
        }

        // 청소 싹
        _monstersInArea.Clear();
    }
}
