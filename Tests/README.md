# 미로 검사

## 순수 C# 회귀 검사

.NET 10 SDK로 저장소 루트에서 실행한다.

```powershell
dotnet run --project Tests/MazeCoreChecks/MazeCoreChecks.csproj
dotnet run --project Tests/MapExplorationChecks/MapExplorationChecks.csproj
```

알고리즘, 변경 순서, 보호/도달성, 지도 탐험 상태 검사다. Unity 물리나 실제 프리팹 검사는 아래와 별도다.

## MainScene 헤드리스 Play Mode 검사

프로젝트와 같은 Unity 6000.3.24f1 및 유효한 라이선스가 필요하다. 같은 프로젝트를 열어 둔 Unity Editor는 먼저 닫는다. 위 회귀 검사를 한 번 실행하면 아래 로그 디렉터리가 만들어진다.

저장소 루트의 PowerShell에서 실행한다. Unity 설치 경로가 다르면 수정한다.

```powershell
$mazeProjectPath = (Get-Location).Path
$mazeUnityArguments = @(
    '-batchmode', '-nographics',
    '-projectPath', ('"{0}"' -f $mazeProjectPath),
    '-executeMethod', 'ShiftingPyramid.EditorChecks.MazeLifecycleSmokeCheck.Run',
    '-logFile', ('"{0}/Tests/MazeCoreChecks/obj/UnityMazeLifecycle.log"' -f $mazeProjectPath)
)
$mazeCheckProcess = Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe' -ArgumentList $mazeUnityArguments -WindowStyle Hidden -PassThru
$mazeCheckProcess.WaitForExit()
$mazeCheckProcess.ExitCode
```

성공 기준은 종료 코드 0과 로그의 `PASS: Unity MainScene ...`이다. 검사 코드가 직접 종료하므로 `-quit`는 넣지 않는다. 실패는 종료 코드 1과 `FAIL:` 또는 Unity 컴파일 오류를 확인한다.

검사 대상:

- 메인 씬의 실제 바닥 표식, 상자·횃불·단지 배치와 Missing Script 여부.
- 상자 보상 지급과 개봉 상태, 단지 획득 개수.
- 부분 변경 시 격자/지도 객체와 개봉 상태/진행도 보존.
- 전체 재생성 시 격자/지도 교체 및 콘텐츠 재배치.
- 잘못된 방 표시 참조와 설정 오류의 사전 차단, 후처리 오류 후 재생성 복구.
- 각 정상 단계에서 실제 NavMesh 시작점→출구 경로가 완전히 연결되는지.

검사에서는 설정 오류와 후처리 예외를 의도적으로 주입한다. 따라서 성공 로그에도 해당 오류 메시지가 포함된다. 씬·프리팹·프로젝트 설정을 저장하지 않으며, 실행 중 복제한 미로 설정만 수정한다. 임시 씬 백업·캐시·컴파일 결과는 Unity가 생성할 수 있다.

한 번의 자동 실행을 전체 게임 QA나 성능 검증으로 보지 않는다. 초기 Agent 경고, 퀴즈/입력, 화면/애니메이션, 장시간 적 이동, 물리적 횃불 배치, 프레임 성능은 별도 확인 대상이다.
