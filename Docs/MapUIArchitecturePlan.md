# 맵 UI 아키텍처 설계

종료/재시작의 현재 기준은 [게임 흐름](GameFlow.md)을 따른다. 플레이어 입력은 `GameplayInput.IsBlocked`에서 지도와 게임 상태를 함께 확인한다. 결과 화면이 뜨면 지도를 먼저 닫고 결과 화면이 시간/커서를 소유하며, 종료 후 맵 닫기로 게임이 재개되지 않는다.

현재 갱신 순서는 [미로 생명주기](MazeLifecycle.md)를 따른다. `MapFloor`는 GUID를 유지한 공용 `Game.World.MazeFloor`로 이동했다. 지도는 `GeometryChanged` 대신 후처리 완료 후의 `MazeChanged`에서 변경 좌표만 대조한다. 아래의 이전 이벤트·타입 설명은 이 기준으로 읽는다.

## 1. 범위와 결정

이 문서는 현재 구현된 맵 UI의 구조와 남은 검증 작업을 정리한다. 맵은 게임 전용 UI로 두고 미로 생성·변경 알고리즘에는 맵 표시나 플레이어 탐험 상태를 넣지 않는다. 코드와 프리팹 연결은 확인했지만 Unity Play Mode에서 화면·입력을 직접 검증한 결과는 아직 없다.

- 원형 미니맵은 화면 한쪽에 고정한다. 북쪽은 항상 위쪽이며 플레이어 마커는 중앙에 고정한다. 플레이어의 연속적인 이동량만큼 지도 내용이 반대 방향으로 움직인다.
- `M` 키로 화면 대부분을 덮는 전체 맵을 토글한다. 전체 맵은 북쪽 고정, 전체 격자 맞춤 보기이며 같은 탐험 기록을 사용한다. 열려 있는 동안 게임을 일시정지한다.
- 플레이어가 밟거나 낮게 점프해 지나갈 때, 발 아래의 실제 미로 바닥이 확인된 격자 칸을 공개한다. 공개되지 않은 칸은 두 맵 모두 바닥·벽·방 종류·출구 정보를 표시하지 않는다. 바닥 탐지 범위 밖을 지나거나 순간 이동한 경로는 공개하지 않는다.
- 미로의 실제 구조가 바뀐 칸은 다시 미탐험으로 만든다. 재방문하면 변경된 구조가 보인다. 미로 변경 시도만 있었거나 결과가 동일한 칸은 유지한다.
- 3x3 방도 칸별로 공개한다. 내부 벽의 정확한 형상은 현재 논리 데이터에 없으므로 1차 맵은 방 내부 구조를 추측해서 그리지 않는다.
- 1차 범위에서 몬스터 위치, 길 안내, 자동 경로 탐색, 개별 저주 단지의 획득 위치 표시는 하지 않는다. 단순한 방/출구 구분도 해당 칸을 밟은 뒤에만 가능하다.

## 2. 현재 코드에서 확인한 사실

| 위치 | 현재 구현 | 남은 확인 사항 |
| --- | --- | --- |
| `Assets/Script/Maze/Core/MazeGrid.cs`, `MazeTile.cs`, `MazeRoom.cs` | 격자 크기, 네 방향 개방 상태, 타일 종류, 3×3 방의 원점·템플릿 인덱스·회전 제공 | 탐험 기록은 게임 계층에서 관리 |
| `Assets/Script/Maze/View/MazeRenderer.cs` | 정수·연속 격자 위치 변환, 방향 변환, 표시 후 `GeometryChanged` 발생 | 변경 좌표 목록은 제공하지 않으므로 맵 상태가 전체 서명을 비교 |
| `Assets/Script/Maze/View/MazeTestBootstrapper.cs` | 생성·검증·렌더링·플레이어 배치 성공 후 `MazeReady` 발생 | 실패·재생성 시 화면 동작은 Play Mode 확인 필요 |
| `Assets/Script/Maze/View/MazeRuntimeChangeController.cs` | 특수방 교체·통로 구역 변경을 시도하고 실제 변경 시 `Refresh` 호출 | 설정에 따라 통로 변경이 불가능할 수 있음 |
| `Assets/Script/Game/Map/` | 탐험 상태, 바닥 탐지, 변경 무효화, 두 지도 뷰, M 키·일시정지·플레이어 입력 차단 구현 | 씬에서 탐지·UI·수명 검증 필요 |
| `Assets/Script/Game/GameManager.cs` | 저주 단지 개수·출구 개방·승패 상태 공개 | 개별 단지 위치는 1차 지도 범위 밖 |
| `Assets/Scenes/MainScene.unity` | `MiniMap`, 기본 비활성 `FullMap`, Canvas의 `MapScreenController` 연결 | 실제 해상도별 레이아웃·버튼 동작 확인 필요 |
| `Assets/Scenes/Maze_Prefab_Scene.unity` | 같은 맵 UI 프리팹과 컨트롤러 연결 | 미로 생성기가 없어 Play Mode에서 지도 데이터는 나타나지 않음 |

`Assets/Data/MazeBalanceSettings.asset`의 현재 크기는 10×10이고 통로 변경 구역 크기는 11이다. 이 설정으로는 통로 변경 무효화 사례를 재현하기 어려우므로 Play Mode 검증 시 구역 크기를 미로 안에 들어가는 홀수로 조정해야 한다. 지도 자체는 미로 크기를 상수로 고정하지 않는다. 메인 씬의 `MazeRuntimeChangeController.mummy` 참조는 비어 있으나 맵 표시의 필수 입력은 아니다.

## 3. 책임과 데이터 흐름

맵 기능은 `Assets/Script/Game/Map/`에 둔다. 일반 타일과 방 바닥에는 `MapFloor`를 부착해 탐지 대상을 표시한다.

| 책임 | 역할 |
| --- | --- |
| `MapTileSignature` | 네 방향 개방, 타일 종류, 방 배치·종류·회전을 값으로 저장한다. |
| `MapExplorationState` | 칸별 공개 여부와 마지막 구조 서명을 보관한다. `Reveal`과 `Reconcile`에서 `TileChanged`를 발생시킨다. |
| `MapStateConnector` | `MazeReady`로 상태를 생성·교체하고 `GeometryChanged`로 실제 변경 칸만 무효화한다. |
| `MapDiscoveryTracker` | 플레이어 발 위치와 아래 방향 바닥 탐지로 방문 칸을 공개한다. |
| `MapGridView`와 `MapTileView` | 공개된 서명만 받아 타일·외곽 벽·타일 종류 색상을 그린다. 두 화면에 각각 뷰가 있지만 탐험 상태는 하나다. |
| `MiniMapController`, `FullMapController` | 미니맵 스크롤·중앙 화살표, 전체 격자 화면 맞춤·위치 화살표를 각각 담당한다. |
| `MapScreenController` | Canvas에서 `M`/닫기 버튼, 전체 맵 활성화, 시간·커서 저장과 복원, 입력 차단 상태를 담당한다. |

데이터는 `MazeGrid` → `MapTileSignature` → `MapExplorationState` → 두 `MapGridView` 순서로 흐른다. 뷰는 미탐험 칸을 조회할 수 없으며 `TileChanged` 좌표만 다시 그린다. 상태가 새 미로로 교체되면 뷰가 기존 UI 타일을 지우고 공개된 칸을 다시 구성한다. 플레이어 위치·방향에 따른 마커와 미니맵 스크롤은 매 프레임 갱신한다.

## 4. 초기 생성과 변경 이벤트

`MazeTestBootstrapper.BuildTestMaze()`는 `mazeRenderer.Build()`를 호출한 **후** `CurrentGrid`를 대입하고 플레이어를 배치한다. `Build()` 안의 `GeometryChanged`는 이보다 먼저 발생한다.

`MazeReady`는 `CurrentGrid`, 방 카탈로그, 시드 대입과 `SpawnAtStart` 성공 후 발생한다. `MapStateConnector`는 이 신호에서 새 `MapExplorationState`를 만들며, 늦게 활성화되면 이미 준비된 `CurrentGrid`를 받는다. 재생성 성공 시 이전 기록은 버리고, 실패 시 준비 완료 신호를 보내지 않는다. 새 상태의 모든 칸은 처음에는 미탐험이다.

런타임에는 특수방 교체와 통로 변경이 실제로 일어난 후 `MazeRenderer.Refresh()`가 `GeometryChanged`를 발생시킨다. `MapStateConnector`는 준비된 현재 미로에 대해서만 `Reconcile`을 호출해 모든 칸의 서명을 비교한다. 초기 `Build()` 이벤트는 `MazeReady` 전이므로 무시한다. 변경 실패나 동일 구조에는 `TileChanged`가 나가지 않는다. 더 큰 미로에서 성능 문제가 확인되기 전까지 변경 좌표 이벤트를 미로 알고리즘에 추가하지 않는다.

## 5. 탐험 및 구조 변경 판정

탐험 상태는 `bool[width, height]`와 같은 크기의 `MapTileSignature` 배열을 사용한다. 각 칸에서 다음을 비교한다.

1. 북·동·남·서 네 방향의 개방 비트.
2. `MazeTileType` (`Normal`, `CurseJarRoom`, `SpecialRoom`, `Exit`).
3. 방 점유 칸이라면 방 원점, `TemplateIndex`, `RotationQuarterTurns`. 일반 칸은 '방 없음'으로 저장한다.

`IsProtected`는 지도 형상을 나타내지 않으므로 비교 대상에서 제외한다. 방 프리팹 이름이나 Unity 인스턴스 ID도 비교하지 않는다. 방 출입구가 템플릿/회전에 의해 정해진다는 현재 모델을 전제로 한다. 나중에 같은 템플릿 인덱스에서 출입구 구성이 런타임에 바뀌면 그 값도 서명에 추가해야 한다.

변경 이벤트마다 모든 칸의 새 서명을 만든다. 이전 서명과 다른 칸만 `explored=false`로 만들고, 화면에서 그 칸의 기존 바닥·벽·색상을 숨긴 뒤 스냅샷을 새 값으로 갱신한다. 원래 탐험하지 않은 칸은 그대로 숨긴다. 통로 양쪽의 공유 벽이 바뀌면 양쪽 칸의 비트가 달라지므로 양쪽을 무효화한다. 공개된 칸의 서명만 벽을 그리는 데 사용하며 미탐험 이웃의 방·목표 정보를 표시하지 않는다. 기존 타일이 그대로라면 단지 '변경 구역에 포함됐다'는 이유로 지우지 않는다.

특수방 이동 시 이전 3x3 자리는 방에서 일반 칸으로, 새 3x3 자리는 일반 칸에서 방으로 바뀐다. 두 발자국의 각 칸을 서명으로 비교하므로 실제로 바뀐 칸만 숨긴다. 플레이어가 변경된 칸 위에 서 있는 예외 상황에서는 무효화가 먼저 일어나고, 그다음 접지 판정에서 현재 칸만 재공개한다. 미로 재생성은 변경이 아니라 새 지도이므로 이전 탐험 기록을 전부 버린다.

## 6. 발 접촉 판정과 지도 좌표

`MazeTestBootstrapper.PlayerTransform`에서 현재 플레이어를 받고 `CharacterController`를 확인한다. 이동 후 `LateUpdate`에서 컨트롤러 바닥 중심을 격자로 변환한다. 직전 발 위치와 현재 위치 사이를 샘플링하며, 격자 기준 이동 거리가 1.5칸을 넘으면 순간 이동으로 보고 중간 경로는 공개하지 않는다. 접지 시에는 0.75, 공중에서는 2.25의 Inspector 탐지 거리로 아래 방향 `RaycastNonAlloc`을 수행한다. 가장 가까운 플레이어 외부 충돌체가 평평한 `MapFloor`이고 샘플 좌표와 맞을 때만 해당 칸을 공개한다. 공중에서 바닥 탐지 범위를 벗어난 칸은 공개하지 않는다. 이 거리는 발밑 탐지 범위이지 실제 접촉 여부의 완전한 물리 증명은 아니다.

일반·출구 타일과 현재 저주 단지·특수방 프리팹의 바닥에는 `MapFloor`가 연결되어 있다. 컨트롤러가 없거나 비활성, 좌표가 격자 밖이거나 바닥 판정이 실패하면 공개하지 않는다. 칸 경계, 작은 단차, 점프 높이에 따른 공개 여부는 Play Mode에서 확인하고 필요하면 탐지 거리·샘플링을 조정한다.

`MazeRenderer.TryGetContinuousGridPosition()`과 `GetGridDirection()`을 UI가 사용한다. 미니맵은 연속 좌표만큼 `MapContent`를 반대로 움직이고 화살표를 중앙에 고정한다. 전체 맵은 격자 전체를 뷰포트에 맞춘 뒤 같은 연속 좌표에 화살표를 놓는다. 격자 Y 증가는 화면 위쪽(북쪽)에 대응하고 지도는 회전하지 않는다.

## 7. Unity UI 구성

`Assets/Prefab/Map/MiniMap.prefab`은 `MiniMap → CircleMask → MapContent`와 중앙 `PlayerArrow`를 사용한다. 원형 `Mask` 밖 타일은 보이지 않아야 한다. `MapStateConnector`와 `MapDiscoveryTracker`는 이 프리팹에 한 번만 있고, 두 `MapGridView`가 같은 상태를 읽는다.

`Assets/Prefab/Map/FullMap.prefab`은 기본 비활성 오버레이이며 `MapViewport → MapContent`, 위치 `PlayerArrow`, 우상단 `CloseButton`을 가진다. `FullMapController`는 뷰포트 크기와 현재 격자 크기로 정사각 타일의 배율을 계산한다. `MapScreenController`는 항상 활성인 Canvas에 붙어 비활성 오버레이를 M 키로 켠다. 두 프리팹은 MainScene과 Maze_Prefab_Scene의 Canvas에 연결되어 있다.

`MapTileUI.prefab`은 바닥 Image와 네 방향 벽 Image로 한 칸을 그린다. 미탐험 칸은 UI 타일 자체를 만들지 않는다. 공개된 `Exit`, `CurseJarRoom`, `SpecialRoom`은 서로 다른 바닥 색으로 구분한다. 색상과 벽 두께는 `MapTileView` Inspector에서 조정한다. 방의 나머지 칸이나 미탐험 목표 위치를 한 칸의 공개만으로 밝히지 않는다.

3x3 방의 현재 논리 격자는 내부 칸이 연결돼 있다고 가정하지만 실제 프리팹 내부 벽·장식의 형상을 저장하지 않는다. 1차 지도에서는 방문한 칸을 방 바닥으로 표시하고 확인 가능한 외곽/출입구만 그린다. 내부 통로가 모두 열려 있다고 그리거나 정확한 실내 평면도라고 주장하지 않는다. 정밀한 방 내부 지도가 필요해지면 프리팹에 별도 맵 표현 데이터를 작성하는 후속 작업으로 분리한다.

## 8. 전체 맵과 일시정지

`MapScreenController`는 `M` 입력 또는 `CloseButton`으로 전체 맵을 전환한다. 열 때 `GameManager`가 있으면 Playing 상태인지 확인하고, 이전 `Time.timeScale`, `Cursor.lockState`, `Cursor.visible`을 저장한다. 맵을 표시한 뒤 시간 배율을 0으로 하고 커서를 푼다. 닫을 때 저장한 값을 복원한다. 컨트롤러 비활성화 시에도 열린 맵을 닫으며, 맵이 열린 동안 승패 상태가 되면 다음 `Update`에서 닫는다. 씬 종료 중 복원 순서는 Play Mode 확인이 필요하다.

`Time.timeScale=0`에도 `Update`와 UI 입력은 실행된다. 현재 `MapScreenController.IsInputBlocked`를 `PlayerMovement`, `MouseLook`, `PlayerInteractor`, `HotbarController`가 확인한다. 핫바 코루틴의 F·Escape 취소 입력도 맵이 열린 동안 무시한다. M 키와 X 버튼은 계속 작동한다. `Escape`는 핫바 취소에 쓰이므로 맵 토글 키로 사용하지 않는다. `PlayerHP`의 테스트용 K 키 피해 입력은 제거했다.

일시정지 중 미로 변경 타이머와 미라화 수치가 증가하지 않는지, 닫은 뒤 이전 시간 배율로 돌아오는지 확인해야 한다. 승패·씬 전환·Canvas 비활성화 시 입력 차단과 커서가 풀린 상태로 남지 않는지도 확인한다. 다른 일시정지 기능이 생기면 동일한 시간·커서 상태를 두 컨트롤러가 덮어쓰지 않도록 소유 규칙을 정한다. `GameNotice`는 실시간 대기 코루틴을 사용하므로 맵 일시정지 중에도 알림이 사라질 수 있다.

## 9. 현재 단계와 남은 검증

| 영역 | 코드·연결 상태 | Play Mode 완료 기준 |
| --- | --- | --- |
| 준비·재생성 | `MazeReady`와 새 탐험 상태 교체 구현 | 생성 실패 후 이전 지도가 새 지도처럼 보이지 않고 재생성 성공 시 기록이 초기화됨 |
| 탐험 | 발 위치 샘플링과 `MapFloor` 바닥 탐지 구현 | 시작 칸, 걷기, 낮은 점프, 높은 점프, 순간 이동, 다른 물체 위 판정이 의도대로 작동 |
| 미니맵 | 원형 마스크·중앙 화살표·연속 스크롤 연결 | 화면 비율 변경 시 원 밖 정보가 보이지 않고 북쪽과 화살표 방향이 맞음 |
| 전체 맵 | 공유 상태 뷰·화면 맞춤·M/X 토글 연결 | 미탐험 정보를 노출하지 않고 두 지도에 같은 공개·무효화 결과가 보임 |
| 일시정지·입력 | 시간·커서 복원과 플레이어 입력 가드 구현 | 열린 동안 이동·시점·상호작용·핫바가 작동하지 않고 닫으면 이전 상태로 복귀 |
| 구조 변경 | 벽·타일 종류·방 배치·템플릿·회전 서명 비교 구현 | 통로 벽 양쪽, 특수방 이전/새 위치만 지워지고 재방문 시 새 구조가 표시됨 |

다음 검증은 MainScene에서 진행한다. 먼저 정상 탐험과 M/X 토글을 확인하고, `MazeBalanceSettings.asset`의 통로 변경 구역 크기를 테스트 가능한 값으로 임시 조정해 벽 변경을 확인한다. 이어서 변경 실패·동일 구조, 3×3 방 교체, 맵을 연 채 승패·씬 전환·Canvas 비활성화, 미로 재생성, 해상도 변경을 확인한다. 설정을 조정했다면 검증 후 원래 밸런스 값으로 되돌린다.

`Tests/MapExplorationChecks`는 미탐험 정보 차단, 공유 벽 양쪽 무효화, 타일 종류·방 템플릿·회전·원점 변경, 재방문·초기화 경계를 검사한다. 물리 바닥 탐지·UI 마스크·버튼·입력 차단·일시정지 복원은 이 독립 테스트로 확인할 수 없으므로 Unity Play Mode 결과가 있어야 완료로 표시한다.

## 10. 공개용 미로 알고리즘과의 경계

`MapExplorationState`, UI 뷰, 입력·일시정지, 플레이어 발견 추적은 전부 게임 전용이다. 공개용으로 분리할 미로 Core/Generation/RuntimeChange/Validation에는 플레이어 방문 기록이나 uGUI 타입을 넣지 않는다. 필요한 작은 통합 신호와 연속 좌표 변환은 Unity 표시·부트스트랩 계층에만 둔다. 맵 UI를 제거해도 미로 생성, 변경, 검증, 렌더링이 그대로 동작해야 한다.
