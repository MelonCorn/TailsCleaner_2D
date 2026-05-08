# 🧹 뽀송세신사

> **2D 뱀서류 액션 생존**  
> 귀여운 라쿤의 뽀송뽀송 청소 대작전! 오염된 탑을 청소하며 몰려오는 적을 처치하는 자동 전투 생존 게임

복잡한 컨트롤 없이 손가락 하나로 이동하고 자동 공격으로 전투합니다.

레벨업마다 스킬을 선택하고, 패시브 태그를 맞춰 강력한 시너지를 시킬 수 있습니다.

장비를 강화, 합성하고 유물을 장착해 캐릭터를 성장시키며 6가지 테마의 오염된 탑을 정복할 수 있습니다.

<br>

| 기간 | 인원 | 엔진 | 플랫폼 |
|------|------|------|--------|
| 2026.02.09 ~ 04.08 (60일) | 기획 7인 · 개발 5인 | Unity 6000.2.10f1 | Android |

**담당** 스킬 시스템 · 사운드

<br>

## 플레이 스토어

[![Google Play](https://img.shields.io/badge/Google_Play-%23414141.svg?style=for-the-badge&logo=google-play&logoColor=white)](https://play.google.com/store/apps/details?id=com.tails.fluffybath&_gl=1*df9r4q*_up*MQ..*_ga*MTA3NDc0ODkzMy4xNzc2ODM3NTc5*_ga_6VGGZHMLM2*czE3NzY4Mzc1NzkkbzEkZzAkdDE3NzY4Mzc1NzkkajYwJGwwJGgw&hl=ko)

<p align="center">
  <img src="https://github.com/user-attachments/assets/4347c550-5119-4806-ae7d-e20ec17481d9" width="32%" alt="Image 1" />
  <img src="https://github.com/user-attachments/assets/182e9001-e26d-4b0a-a070-3aa36d6541ce" width="32%" alt="Image 2" />
  <img src="https://github.com/user-attachments/assets/3fc4903f-6e3e-4dd7-ab98-9ecdc38bfe1c" width="32%" alt="Image 3" />
</p>

<br>

## 📌 목차

- [게임 소개](#-게임-소개)
- [게임 플로우](#-게임-플로우)
- [핵심 구현](#-핵심-구현)
  - [확장성 높은 스킬 코어 로직](#1-확장성-높은-스킬-코어-로직)
  - [비트 기반 태그 매칭](#2-비트-기반-태그-매칭)
  - [조립식 스킬 모디파이어](#3-조립식-스킬-모디파이어)
  - [스탯 계산 파이프라인](#4-스탯-계산-파이프라인)
  - [시각 연출 최적화](#5-시각-연출-최적화)
- [스킬 설계 선택 — Generic vs 개별 상속](#스킬-설계-선택--generic-vs-개별-상속)
- [트러블슈팅](#-트러블슈팅)

<br>

---

## 🎮 게임 소개

| 핵심 요소 | 설명 |
|----------|------|
| **한 손 캐주얼 액션** | 손가락 하나로 이동 + 자동 공격이 가능한 직관적인 조작 |
| **탑 & 보스** | 6가지 테마의 오염된 탑에서 버티며 청소해 보스를 처치 |
| **스킬 강화 & 시너지** | 레벨업마다 액티브·패시브 스킬 선택, 패시브 태그를 맞추면 모든 액티브 스킬이 동시 강화 |
| **장비 제작 & 유물** | 전투 보상으로 장비를 강화·합성하고 유물을 장착해 영구 성장 |

<br>

---

## ⚙️ 핵심 구현

### 1. 확장성 높은 스킬 코어 로직

**문제** 스킬마다 투사체 유형과 모디파이어 타입이 달라 타입 캐스팅 발생.  
스킬 종류가 늘수록 분기와 캐스팅 비용이 함께 증가

**해결** 스킬을 제네릭으로 묶어 타입 캐스팅 제거

```csharp
// 새 스킬 추가 시 코어 코드 무수정
public class FireSkill : ActiveSkill<FireProjectile, FireModifierData> { ... }
public class IceSkill  : ActiveSkill<IceProjectile,  IceModifierData>  { ... }
```

- `ActiveSkill<TProj, TMod>` — 투사체는 서브클래스, 모디파이어는 순수 데이터 클래스로 분리
- 투사체 클래스가 자신의 행동을 결정 / 모디파이어는 플래그와 수치만 보유
- 업그레이드 시 모디파이어가 플래그 세팅

→ 불필요한 타입 캐스팅 완전 제거 / 충돌 없이 팀원 독립 스킬 작업 가능

<br>

### 2. 비트 기반 태그 매칭

**문제** 메인·서브 태그 조합·시너지 조건이 수십 가지.  
조건마다 분기 시 코드 복잡도가 심해짐

**해결** 태그 조합 판별을 비트 연산으로 처리, 새 태그 추가 시에도 로직 수정 최소화

```csharp
private void AddSubTag(ActiveUpgradeData upgradeData)
{
    if (upgradeData.SubTag1 != 0) CurrentSubTag |= SubTagRegistry.GetFlag(upgradeData.SubTag1);
    if (upgradeData.SubTag2 != 0) CurrentSubTag |= SubTagRegistry.GetFlag(upgradeData.SubTag2);
}
```

| 단계 | 연산 | 용도 |
|------|------|------|
| 스킬 습득 | OR | 플래그 누적 |
| 시너지 체크 | AND | 조건 판별 |
| 선택지 필터 | AND | 태그 기반 후보 선별 |

→ 태그 조합 판별 O(1), 루프·분기 없음 / 시너지 조건 기획 변경 시 플래그 값만 수정

<br>

### 3. 조립식 스킬 모디파이어

**문제** 업그레이드 기믹이 늘어날수록 스킬 클래스 내부 분기(`if/switch`) 증가.  
기믹 추가마다 스킬 직접 수정 필요

**해결** 기믹 로직을 스킬과 분리해 독립 모듈로 부착

- 제네릭 모디파이어를 상속 후 업그레이드 적용 메서드 구현
- 업그레이드 시 플래그와 수치를 세팅, 현재 업그레이드 단계 조회
- 레벨 비례 수치 적용

→ 새 기믹 추가 = 모디파이어 서브클래스 1개 + 모디파이어 데이터 필드 1개 / 스킬 클래스 코드 수정 없음

<br>

### 4. 스탯 계산 파이프라인

**문제** 업그레이드·패시브가 쌓이며 스탯 계산식이 복잡해짐.  
스킬 발동마다 매 프레임 전체 파이프라인을 재실행하면 CPU 낭비.  
스탯 변경 시점을 놓치면 스탯 불일치 버그 발생

**해결** 스탯이 실제로 변한 시점에만 재계산, 결과를 캐싱

```
baseStat → passiveBase → commonStat → upgradeStat → passiveMultiply → finalMultiply
  (기본)    (+패시브 스탯)  (*공용 배율)  (+전용 수치)   (*패시브 배율)    (*최종 배율)
```

최종 스탯 계산식
```
((기본 스탯 + 패시브 기본 스탯) × 공용 업그레이드 배율 + 전용 업그레이드 수치) × 패시브 배율 × 최종 배율
```

→ 패시브·업그레이드 추가 시에만 파이프라인 1회 실행 / 스킬 수 증가·패시브 중첩에도 런타임 연산 비용 고정

<br>

### 5. 시각 연출 최적화

**문제** 투사체 수백 개 동시 사용 시 Unity 내장 `Animator` 오버헤드 과다

**해결** `Animator` 없이 커스텀 스프라이트 애니메이터 구현 + Sprite Atlas 적용

- 코드로 직접 스프라이트 배열 순회 재생
- SO로 스킬별 스프라이트·속도·루프 설정 분리
- Atlas 적용으로 동일 Atlas 내 스프라이트 Draw Call 통합

| 측정 항목 | 적용 전 | 적용 후 |
|----------|--------|--------|
| SetPass Calls | 38 | 18 |
| Batches | 46 | 25 |
| **감소율** | | **약 50%** |

→ `Animator` 컴포넌트 완전 제거 / 스킬 비주얼 설정 시 인스펙터 SO 편집만으로 완료

<br>

---

### 스킬 설계 선택 — Generic vs 개별 상속

| | 개별 상속 | **Generic ✅** |
|---|---|---|
| 스킬 N종 | 클래스 N개 생성 | 서브클래스 1개 |
| 타입 캐스팅 | 모디파이어마다 캐스팅 필요 | 완전 제거 |
| 코어 수정 | 스킬 추가마다 위험 | 무수정 |
| 모디파이어 조합 | 제한적 | 자유도 극대화 |
| 초기 설계 비용 | 낮음 | 타입 파라미터 설계 필요 |

**결론** 스킬 종류가 계속 추가되는 구조에서 타입별 클래스 증식은 유지보수 난이도 급증.  
제네릭 설계로 코어를 단일화하고 모디파이어를 조합하는 방식으로 개발 속도와 확장성을 동시에 확보.

<br>

---

## 🐛 트러블슈팅

### #1 — 장판형 스킬 탐색 연산 과부하

| | |
|--|--|
| **원인** | 틱마다 `OverlapCircleAll` 호출 → 장판 10개 × 몬스터 수십 마리 = 매 프레임 수백 회 물리 쿼리. GC 스파이크 + CPU 부하로 프레임 드랍 |
| **해결** | `ContactFilter2D` + `HashSet<Collider2D>`로 적 캐싱 구조 도입. `OnTriggerEnter2D`에서 추가 / `OnTriggerExit2D`에서 제거, 틱 데미지는 HashSet 순회로 적용. 비활성 스킬 오브젝트는 틱 코루틴 중단 |
| **결과** | `OverlapCircleAll` 호출 제거로 GC Alloc 감소 / 장판 수십 개 동시 활성 환경에서도 프레임 드랍 해소 |

<br>

### #2 — 물리 충돌 레이어 안정화

| | |
|--|--|
| **원인** | 모든 오브젝트가 Default 레이어 → 투사체가 투사체·경험치·플레이어에 충돌하는 등 불필요한 충돌 판정 폭증 |
| **해결** | 레이어 세분화 (`Player` / `Monster` / `Projectile` / `Item` / `SkillArea`). Physics 2D Collision Matrix에서 불필요한 연결 해제, 스크립트에서도 `LayerMask` 명시적 지정으로 이중 차단 |
| **결과** | 충돌 판정 수 대폭 감소 / 물리 CPU 연산량 안정화 |

<br>
