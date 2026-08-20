# BUILD.md — 빌드 & CI/CD 파이프라인 문서

이 문서는 "알빠임? 용병단 -시너지 디펜스-"(Who cares? Mercenary Corps! - Synergy Defense) 프로젝트를
빌드하는 방법과, GitHub Actions로 자동화된 CI/CD 파이프라인의 구조를 정리한다.

## 1. 빌드 환경

| 항목 | 값 |
|---|---|
| 엔진 | Unity **6000.2.6f2** |
| 렌더 파이프라인 | URP (Universal Render Pipeline) |
| 대상 플랫폼 | Android (arm64), Windows Standalone |
| 빌드 진입점 | `Assets/Scripts/Editor/BuildScript.cs` → `BuildScript.BuildAndroid()` |
| 출력 경로 | `Builds/Android/WhocaresMercenaryCorps_<bundleVersion>.apk` |

로컬(에디터 UI)에서 빌드하려면 메뉴 `Build → Android APK`.
커맨드라인(배치 모드)에서 빌드하려면:

```
"C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe" ^
  -batchmode -quit ^
  -projectPath "C:\Users\jiseo\My_Game" ^
  -buildTarget Android ^
  -executeMethod BuildScript.BuildAndroid ^
  -logFile "C:\Users\jiseo\My_Game\build.log"
```

## 2. 빌드 전 자동 처리 — CSV → Resources 동기화

`Assets/Scripts/Editor/CsvResourceSync.cs`가 `IPreprocessBuildWithReport`로 등록되어 있어,
**모든 빌드(로컬/CI 공통) 직전에 자동으로** 프로젝트 루트의 `zombies.csv` / `rounds.csv`를
`Assets/Resources`와 `Assets/StreamingAssets`로 복사한다. 안드로이드에서 StreamingAssets를
`System.IO`로 직접 읽을 수 없는 문제(6절 참고)를 우회하기 위한 장치이므로, 밸런스 데이터는
프로젝트 루트의 CSV만 수정하면 별도 조치 없이 다음 빌드에 반영된다.

## 3. CI/CD 파이프라인 (`.github/workflows/android-build.yml`)

### 3.1 트리거

| 이벤트 | 조건 |
|---|---|
| `push` | `main`, `develop` 브랜치 |
| `pull_request` | `main`을 대상으로 하는 PR |
| `workflow_dispatch` | 수동 실행 |

같은 브랜치/PR에 대한 중복 실행은 `concurrency` 그룹으로 취소된다 (`cancel-in-progress: true`).

### 3.2 Job 구성 (3단계, 순차 의존)

```
validate-balance-data  →  build-android  →  release (main push에서만)
```

1. **validate-balance-data** — `python3 tools/validate_csv.py`로 `zombies.csv`/`rounds.csv`
   무결성 검사 (아래 4절). Unity를 아예 띄우기 전에 실행되므로, 데이터 문제로 인한
   빌드 시간 낭비를 막는다.
2. **build-android** — `game-ci/unity-builder@v4`로 Android APK 빌드. 내부적으로
   `BuildScript.BuildAndroid()`를 호출한다(로컬 빌드와 동일 코드 경로). `Library` 폴더를
   `actions/cache`로 캐싱해 재-임포트 시간을 줄인다. 결과 APK는 `actions/upload-artifact`로
   14일간 보관된다.
3. **release** — `main` 브랜치로의 push일 때만, 빌드된 APK를 GitHub Release로 발행한다
   (`softprops/action-gh-release`, 태그는 `build-<run_number>`). README에 안내된
   "Releases 탭에서 APK 다운로드" 배포 방식과 직결된다.

### 3.3 버전 관리 — 별도 스크립트 없이 CI 도구의 시맨틱 버저닝을 사용

처음엔 버전 자동 증가용 스크립트를 따로 만들려고 했으나, `game-ci/unity-builder`가
기본으로 제공하는 **Semantic 버저닝 전략**이 git 브랜치/커밋 정보로부터 버전과
`androidVersionCode`를 자동으로 계산해준다는 걸 실제 빌드 로그에서 확인했다:

```
Found semantic version 1.0.0 for feature/ci-cd-pipeline@0bcd97e
Using android versionCode 1000000
```

즉 커밋을 push할 때마다 버전 코드가 자동으로 올라간다. 이미 검증된 도구가 하는 일을
다시 스크립트로 만드는 대신, 이 동작을 그대로 채택하고 여기 문서화하는 쪽을 택했다.
프로젝트 파일(`ProjectSettings.asset`)의 `bundleVersion`/`AndroidBundleVersionCode`는
정식 배포(스토어 업로드) 시점에만 수동으로 올린다.

### 3.4 필요한 GitHub Secrets

| Secret | 용도 | 상태 |
|---|---|---|
| `ANDROID_KEYSTORE_BASE64` | 릴리즈 서명 키스토어(base64) | ✅ 등록 완료 |
| `ANDROID_KEYSTORE_PASS` | 키스토어 비밀번호 | ✅ 등록 완료 |
| `ANDROID_KEY_ALIAS` | 키 alias (`whocares-release`) | ✅ 등록 완료 |
| `ANDROID_KEY_ALIAS_PASS` | 키 비밀번호 | ✅ 등록 완료 |
| `UNITY_LICENSE` | Unity 라이선스 파일(.ulf) 내용 | 수동 활성화 절차 진행 중 |

키스토어는 `keytool`로 신규 생성했고(PKCS12, alias `whocares-release`), 로컬에는
`keystore/`(`.gitignore`에 등록되어 git에는 올라가지 않음)에 백업되어 있다.

Unity 라이선스는 계정 비밀번호를 CI에 직접 저장하는 대신, 수동 활성화 파일(.alf) →
license.unity3d.com에서 발급받은 라이선스 파일(.ulf) 내용을 `UNITY_LICENSE` 시크릿에
등록하는 방식을 택했다 — 계정 자격증명 자체를 어디에도 저장하지 않기 위함이다.

## 4. 밸런스 데이터 검증 (`tools/validate_csv.py`)

CI의 첫 단계로 실행되며, 아래를 검사한다.

- `zombies.csv`: `UID` 비어있지 않음/중복 없음, `Hp`/`Speed`/`damage`/`attackCooldown`/`attackRange`가
  숫자이고 음수가 아님 (Hp는 0도 거부)
- `rounds.csv`: `round` 값이 1부터 연속, 각 `Spawn_N`이 `zombies.csv`에 실제로 존재하는 UID를
  참조, `Spawn_N_min <= Spawn_N_max`, 헤더가 정의한 컬럼 수를 초과하는 필드가 없는지(초과 시
  DictReader가 값을 조용히 버리는 문제를 별도로 탐지)

이 스크립트를 실제 데이터에 돌려서 기존에 있던 버그 2건을 찾았다 (5절 참고).

## 5. 발견된 이슈 — rounds.csv 데이터 버그 2건 (2026-08-20, 미해결)

CI에 검증 스크립트를 붙이자마자 실데이터에서 아래 2건이 발견됐다. 게임 밸런스 데이터라
디자인 판단이 필요해 즉시 고치지 않고, 대신 해당 스텝만 `continue-on-error: true`로
완화해서(로그에는 계속 남는다) 나머지 파이프라인(빌드/릴리즈)은 계속 검증할 수 있게 했다.

- **round 10**: `Spawn_5='boss1'`가 `zombies.csv`에 존재하지 않는 UID. 오탈자이거나,
  아직 `zombies.csv`에 등록되지 않은 보스를 참조 중인 것으로 추정.
- **round 20**: 헤더가 정의한 7개 Spawn 슬롯을 초과하는 8번째 그룹(`ooze_boss`)이
  들어있어, CSV를 표준 방식(`csv.DictReader`)으로 읽으면 이 값이 조용히 버려지고 있었다
  (파싱 에러 없이 사라지므로 지금까지 아무도 몰랐을 가능성이 있음).

`.github/workflows/android-build.yml`의 `Run CSV validation` 스텝에 있는
`continue-on-error: true`와 관련 TODO 주석은, 이 데이터가 수정되면 반드시 제거해야 한다.

## 6. 트러블슈팅 사례 — Android에서 StreamingAssets를 못 읽던 문제

`CsvResourceSync.cs`에 남아있는 주석에 기록된, 이미 해결된 실제 버그.

**증상**: PC 빌드에서는 정상이었는데, Android 빌드에서는 좀비가 한 마리도 스폰되지 않음.
소환 마법진 연출은 정상적으로 재생되는데 그 이후 아무 일도 일어나지 않아서, 처음엔
스폰 로직이나 애니메이션 이벤트 쪽 문제로 오인하기 쉬운 증상이었다.

**원인**: `Application.streamingAssetsPath`가 Android에서는 실제 파일 경로가 아니라
`jar:file:///.../base.apk!/assets` 형태의 URL이 되는데, `System.IO.File.ReadAllText` 같은
일반 파일 API로는 이 경로를 읽을 수 없다. 그 결과 `zombies.csv` 로드가 조용히 실패해서
좀비 정의가 0개가 되었고, 스폰 시도 자체가 무의미해진 것 — "연출만 나오고 적이 안 나온다"는
증상은 소환 이펙트가 스폰 시도보다 먼저 재생되는 순서 때문이었다.

**해결**: `CsvResourceSync.cs`가 빌드 직전(`IPreprocessBuildWithReport`)에 CSV를
`Assets/Resources`로도 복사해두고, 런타임 로더가 파일 경로 → `Resources.Load<TextAsset>`
순으로 폴백하도록 변경. `Resources`는 모든 플랫폼에서 동일하게 동작하므로 플랫폼별 분기
없이 해결됐다.

**재발 방지**: 이 동기화가 사람이 깜빡하고 건너뛸 수 있는 수동 단계로 남아있으면 같은 문제가
재발할 수 있어서, 빌드 프리프로세서 훅으로 만들어 로컬 빌드든 CI 빌드든 항상 자동으로
실행되게 했다. 이번에 CI를 구성하면서도 별도 스텝 없이 그대로 재사용했다.
