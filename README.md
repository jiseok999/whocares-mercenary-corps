# 알빠임? 용병단 -시너지 디펜스-

> Who cares? Mercenary Corps! — Synergy Defense

용병을 뽑고, 합성하고, 배치해서 밀려오는 오크 군단을 막아내는 **시너지 디펜스** 게임입니다.
상점에서 용병을 사서 3개를 모아 합성하고, 보드 위 조합으로 시너지를 완성해 30라운드를 버텨냅니다.

- **플레이 빌드(APK)**: 이 저장소의 [Releases](../../releases) 탭에서 내려받을 수 있습니다.

---

## 심사자용 안내 (플레이 방법)

### Android APK

1. 우측 [Releases](../../releases)에서 최신 `WhocaresMercenaryCorps_x.y.z.apk`를 내려받습니다.
2. 안드로이드 기기에서 설치합니다. 스토어를 거치지 않은 APK이므로 설치 시
   **"출처를 알 수 없는 앱 설치 허용"**을 한 번 켜주셔야 합니다.
   (설정 → 앱 → 특별한 앱 접근 → 알 수 없는 앱 설치 → 사용한 브라우저/파일 관리자 허용)
3. 별도의 로그인·결제·유료 라이선스 없이 바로 플레이할 수 있습니다.

- 요구 사양: Android 6.0(API 23) 이상, **가로 모드 전용**
- 최초 실행 시 튜토리얼 성격의 1~3라운드가 진행됩니다.

### 조작

터치 기반입니다. 상점에서 용병을 골라 보드로 **드래그**해 배치하고, 같은 용병이 모이면
자동으로 진화됩니다. 배치된 용병을 길게 눌러 옮기거나 벤치로 되돌릴 수 있습니다.

---

## 게임 개요

| 항목 | 내용 |
| --- | --- |
| 장르 | 오토배틀러 + 디펜스 (시너지 조합) |
| 플레이 타임 | 1회차 약 20~30분 |
| 라운드 | 30라운드 (`rounds.csv`로 구성) |
| 적 유형 | 일반 오크 10종 + 보스 3종(데몬 / 우즈 / 네크로맨서) |
| 지원 언어 | 한국어, 영어, 일본어, 번체/간체 중국어, 태국어, 스페인어, 포르투갈어, 프랑스어, 이탈리아어, 독일어 (11개) |

라운드마다 파티 타입(균형·물량·장갑·마법·관통·보스 등)이 정해지고, 그에 맞춰 적 구성이 달라집니다.
플레이어는 라운드 사이 휴식 시간에 상점을 열어 용병을 보강하고 시너지를 다시 짭니다.

---

## 개발 환경

- **Unity 6000.2.6f2**
- Universal Render Pipeline (URP)
- Input System (신규) + 레거시 Input Manager 병행 (`activeInputHandler: 2`)
- 대상 플랫폼: Android (arm64), Windows Standalone

## 프로젝트 구조

```
Assets/
  Scripts/
    Board/          보드·벤치·필드 경계 생성 및 좌표 계산
    Characters/     아군 용병 로직 (전투, 드래그 배치, 합성, 투사체)
    Zombies/        적 로직, 웨이브 구성, CSV 데이터 로더
    Traits/         시너지(특성) 판정 및 발동 효과
    Shop/           상점, 등급 확률, 용병 대사
    UI/             HUD, 로컬라이제이션, 툴팁, 옵션
    Meta/           도감·각성·해금 등 메타 진행
    Editor/         빌드 자동화, CSV 동기화, 이미지 익스포터
    GameCameraFit.cs   고정 16:9 레터박스 카메라 유틸
  Resources/        런타임 로드 스프라이트·사운드·CSV
  Scenes/           Title / Lobby / Game
tools/              로컬라이제이션 테이블 생성용 Python 스크립트
zombies.csv         적 기본 스탯 (밸런싱 원본)
rounds.csv          라운드별 스폰 구성 (밸런싱 원본)
```

### 데이터 주도 밸런싱

적 스탯과 라운드 구성은 프로젝트 루트의 `zombies.csv` / `rounds.csv`에 있습니다.
이 두 파일은 빌드 시 `Assets/Resources`와 `Assets/StreamingAssets`로 자동 복사됩니다
(`Assets/Scripts/Editor/CsvResourceSync.cs`).

> 모바일 빌드에서는 StreamingAssets가 APK 내부에 압축되어 `System.IO`로 읽을 수 없기 때문에,
> 런타임 로더는 **파일 경로 → Resources(TextAsset)** 순으로 폴백하도록 되어 있습니다.

## 빌드 방법

1. Unity 6000.2.6f2로 프로젝트를 엽니다.
2. 하단 서드파티 에셋 안내를 확인합니다.
3. Android APK: 상단 메뉴 **Build → Android APK** (`Assets/Scripts/Editor/BuildScript.cs`)
   - 또는 CLI: `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildAndroid`
   - 결과물은 `Builds/Android/`에 생성됩니다.

---

## 서드파티 에셋 고지

이 저장소에는 아래 Unity 에셋스토어 패키지가 포함되어 있습니다.
게임 실행에 필요한 구성 요소로서 포함한 것이며, 각 패키지의 저작권은 원저작자에게 있습니다.
**패키지 자체를 재사용·재배포할 목적으로 내려받지 말아 주세요.**

| 패키지 | 용도 | 위치 |
| --- | --- | --- |
| SPUM (Sprite Pixel Unit Maker) | 아군 용병 캐릭터 아트 및 애니메이션 | `Assets/SPUM/` |
| Casual Game Sounds U6 | 효과음 | `Assets/Casual Game Sounds U6/` |

폰트는 [둥근모꼴(neodgm)](https://cactus.tistory.com/193)을 사용했습니다.

## 저장소 이력에 대하여

이 프로젝트는 개발 기간 동안 **Plastic SCM**으로 형상 관리를 해왔고,
제출을 위해 git으로 이전하면서 저장소를 새로 시작했습니다.
따라서 초기 커밋 시점 이전의 변경 이력은 이 저장소에 남아 있지 않습니다.
이후의 모든 작업은 이 저장소에 커밋으로 기록됩니다.

## 라이선스

게임 소스 코드 및 자체 제작 에셋의 저작권은 제작자(Metal_Pangja)에게 있습니다.
위에 명시된 서드파티 에셋은 각 패키지의 라이선스를 따릅니다.
