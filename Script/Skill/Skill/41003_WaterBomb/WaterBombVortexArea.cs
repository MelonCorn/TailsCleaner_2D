using System.Collections.Generic;
using UnityEngine;

public class WaterBombVortexArea : PoolObject
{
    private float _pullInterval;    // 당기기 주기
    private float _lastPullTime;    // 최근 당기기 시간
    private int _remainPullCount;   // 남은 당기기 횟수

    private bool _bulletClear;      // 폭발은 예술이다 상태
    private bool _expired;          // 만료 상태

    private List<PassiveModifier> _passiveModifiers;    // 패시브

    // 범위 내 몬스터
    private HashSet<MonsterBase> _monstersInArea = new();

    // 범위 내 적 투사체
    private HashSet<MonsterProjectile> _bulletsInArea = new();

    private Rigidbody2D _rigidbody;
    private PoolObject _poolObject;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _poolObject = GetComponent<PoolObject>();
    }


    public void Init(float pullInterval, int pullCount, float size, List<PassiveModifier> passives, bool bulletClear)
    {
        _pullInterval = pullInterval;
        _remainPullCount = pullCount;
        _lastPullTime = Time.time;
        _bulletClear = bulletClear;
        _expired = false;
        _passiveModifiers = new List<PassiveModifier>(passives);

        transform.localScale = Vector3.one * size;
    }

    private void Update()
    {
        if (_expired) return;

        // 남은 횟수만큼 끌어당기기
        if (_remainPullCount > 0 && Time.time >= _lastPullTime + _pullInterval)
        {
            //Debug.Log($"남은 소용돌이 틱 수 : {_remainPullCount}");
            // 끌어당기고
            ProcessPull();
            // 횟수 차감
            _remainPullCount--;
            // 주기 갱신
            _lastPullTime = Time.time;
        }

        // 남은 횟수 없으면 비활성화
        if (_remainPullCount <= 0)
        {
            //Debug.Log("소용돌이 종료");
            // 만료 상태 전환
            _expired = true;
            // 해시셋 정리
            _monstersInArea.Clear();
            _bulletsInArea.Clear();

            // 풀 반환 실패 시 파괴
            if (_poolObject != null) _poolObject.ReturnToPoolAfter(0);
            else Destroy(gameObject);

            return;
        }
    }


    // 끌어당기기
    private void ProcessPull()
    {
        // null 지우기
        _monstersInArea.RemoveWhere(monster => monster == null || monster.gameObject.activeInHierarchy == false);
        _bulletsInArea.RemoveWhere(bullet => bullet == null || bullet.gameObject.activeInHierarchy == false);

        // 범위 내 적, 투사체 끌어당기기
        foreach (var monster in _monstersInArea)
            monster.Pull(_rigidbody.position);

        // 범위 내 적, 투사체 끌어당기기
        foreach (var bullet in _bulletsInArea)
            bullet.Pull(_rigidbody.position);
    }

    // 폭발은 예술이다
    // 투사체 진입 즉시 삭제 + 방어막 획득
    private void OnTriggerEnter2D(Collider2D col)
    {
        // 몬스터
        if (col.CompareTag("Monster"))
        {
            // 컴포넌트
            if (col.TryGetComponent(out MonsterBase monster))
            {
                // 해시셋에 등록
                _monstersInArea.Add(monster);
            }
        }
        // 몬스터 투사체
        else if (col.CompareTag("MonsterBullet"))
        {
            // 컴포넌트
            if (col.TryGetComponent(out MonsterProjectile projectile))
            {
                // 폭발은 예술이다
                if(_bulletClear == true)
                {
                    // 반환
                    if (projectile is PoolObject)
                        projectile.ReturnToPoolAfter(0);

                    // 플레이어 방어막 획득
                    SkillManager.Instance.SkillStatHandler.TryAddShield(1);
                    // 해시셋 추가는 스킵
                    return;
                }

                // 폭발은 예술이다 비활성화면
                // 해시셋에 추가
                _bulletsInArea.Add(projectile);

            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    { // 몬스터
        if (col.CompareTag("Monster"))
        {
            // 컴포넌트
            if (col.TryGetComponent(out MonsterBase monster))
            {
                // 해시셋에서 삭제
                _monstersInArea.Remove(monster);
            }
        }
        // 몬스터 투사체
        else if (col.CompareTag("MonsterBullet"))
        {
            // 컴포넌트
            if (col.TryGetComponent(out MonsterProjectile projectile))
            {
                // 해시셋에서 삭제
                _bulletsInArea.Remove(projectile);
            }
        }
    }
}
