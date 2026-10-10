# 미로 시스템 아키텍처 메모

이 프로젝트는 나중에 GitHub에 올릴 때도 Unity 프로젝트 형태로 공개할 가능성이 높다.
그래서 미로 코드를 완전히 독립 C# 라이브러리처럼 과하게 분리하지는 않는다.

대신 아래 기준만 지킨다.

```text
Unity 프로젝트 안에서 개발한다.
하지만 미로 계산 코드와 Unity 표시 코드는 섞지 않는다.
```

## 현재 구현된 파일

```text
Assets/Script/Maze/Core/MazeCoordinate.cs
Assets/Script/Maze/Core/MazeDirection.cs
Assets/Script/Maze/Core/MazeTile.cs
Assets/Script/Maze/Core/MazeTileType.cs
Assets/Script/Maze/Core/MazeGrid.cs
Assets/Script/Maze/Core/MazeRoom.cs
Assets/Script/Maze/Generation/DepthFirstMazeGenerator.cs
Assets/Script/Maze/Generation/MazeRoomPlacer.cs
Assets/Script/Maze/Generation/MazeExitPlacer.cs
Assets/Script/Maze/Validation/MazePathValidator.cs
Assets/Script/Maze/Settings/MazeBalanceSettings.cs
Assets/Script/Maze/View/MazeTileView.cs
Assets/Script/Maze/View/MazeRoomView.cs
Assets/Script/Maze/View/MazeRoomEntranceMarker.cs
Assets/Script/Maze/View/MazeRenderer.cs
Assets/Script/Maze/View/MazeTestBootstrapper.cs
```

## 폴더 기준

```text
Assets/Script/Maze/Core
미로 좌표, 방향, 타일, 그리드 같은 계산용 코드

Assets/Script/Maze/Settings
미로 크기, 방 개수, 변경 주기 같은 밸런싱 설정 에셋

Assets/Script/Maze/Generation
초기 미로 생성 알고리즘

Assets/Script/Maze/Validation
길이 이어져 있는지 검사하는 알고리즘

Assets/Script/Maze/RuntimeChange
게임 중 일부 구역의 벽을 바꾸는 로직

Assets/Script/Maze/View
미로 데이터를 Unity 오브젝트로 보여주는 코드
```

## 분리 기준

`Maze/Core`, `Generation`, `Validation` 쪽은 가능하면 아래 Unity 요소에 직접 의존하지 않는다.

```text
GameObject
MonoBehaviour
Transform
Collider
Player
Mummy
Treasure
Exit
UI
```

대신 필요한 정보는 `MazeCoordinate` 같은 좌표 데이터로 넘긴다.

예시:

```text
나쁜 방식:
MazeGenerator가 TreasureRoom 오브젝트를 직접 찾는다.

좋은 방식:
게임 쪽에서 보물방 좌표 목록을 만들고,
미로 쪽에는 보호 좌표 목록으로 넘긴다.
```

## 밸런싱 설정

`MazeBalanceSettings`는 Unity `ScriptableObject`다.

Unity에서 생성:

```text
Project 창 우클릭
-> Create
-> Shifting Pyramid
-> Maze Balance Settings
```

현재 들어간 값:

```text
mazeWidth
mazeHeight
treasureRoomCount
specialRoomCount
mazeChangeRegionSize
mazeChangeInterval
playerSafeRadius
mummySafeRadius
useRandomSeed
seed
```

`requiredTreasureCount`처럼 승리 조건에 직접 들어가는 게임 룰은 `GameManager`에서 관리한다.
나중에 GitHub 공개용으로 다듬을 때는 `MazeBalanceSettings`와 미로 알고리즘 쪽만 남기고, `GameManager` 같은 게임 전용 코드는 제외한다.

## 현재 방 구현

현재는 3x3 보물방·특수방 영역을 먼저 예약하고, 일반 칸과 방을 각각 방문 단위로 삼아 DFS로 통로를 생성한다.
표시한 출입구는 모두 연결하고 이후 출구를 배치한다. 보물방·특수방은 9칸 전체를 보호 대상으로 표시한다.
특수방의 런타임 등장·제거는 아직 구현되지 않았다.
개수는 `MazeBalanceSettings.TreasureRoomCount`와 `SpecialRoomCount`로 관리한다.

## 현재 방 프리팹 연결

씬의 `MazeRenderer` 컴포넌트에서 `Tile Prefab`은 일반 타일로 연결한다.
`Assets/Data/MazeBalanceSettings.asset`의 `Treasure Room 3x3 Prefab`과 `Special Room 3x3 Prefabs`에는 루트에 `MazeRoomView`가 붙은 큰 방을 연결한다.
`Exit Room Prefab`에는 기존 1칸 출구방의 `MazeTileView`를 연결한다.
큰 방에는 `MazeTileView`를 사용하지 않는다. 기존 1칸 방 참조는 숨겨서 보존하고 새 생성에 사용하지 않는다.
방 개수가 양수인데 큰 방 프리팹이 비어 있으면 생성 오류를 표시한다. 출구 프리팹이 비어 있으면 일반 타일로 표시한다.
초기 생성 시에는 방과 통로를 연결한 뒤 시작점에서 통로 거리상 가장 먼 가장자리 칸을 `Exit` 타일로 지정한다.
출구 칸은 보호 대상으로 표시하고, 미로 바깥을 향한 벽 하나를 연다.
`Exit_Tile.prefab` 루트의 트리거와 `ExitGate`가 플레이어 진입 시 `GameManager.TryEscape()`를 호출한다.
탈출 가능 여부는 `GameManager`가 보물 획득 상태로 판정하며, 잠금 상태를 나타내는 물리적 문은 아직 없다.

## 구현된 구조: 직접 제작하는 3x3 방 프리팹

보물방과 특수방은 각각 일반 미로 타일 9칸을 차지하는 프리팹 하나로 제작한다.
예약·생성·검증·렌더링 코드를 이 방식으로 전환했다. 제작한 방 프리팹은 아래 컴포넌트를 붙이고 설정 에셋에 직접 연결해야 한다.
구체적인 제작/연결 순서는 [RoomPrefabSetup.md](RoomPrefabSetup.md)를 참고한다.
일반 통로는 기존 타일의 네 방향 벽 개폐 방식을 유지하고, 방 내부와 문은 제작자가 직접 구성한다.

### 프리팹 제작 기준

- 가로·세로는 각각 `tileSize * 3`. 현재 타일 간격 10 기준으로 30x30 크기다.
- 루트 피벗은 방 중앙의 바닥에 둔다.
- 바닥, 외벽, 천장, 내부 장식과 실제 문 오브젝트를 직접 만든다.
- 방이 차지하는 9칸에는 일반 타일 프리팹을 중복 생성하지 않는다.
- 문 위치에는 출입구 표시점을 두고, 표시점마다 방 내부의 경계 칸 좌표와 바깥 방향을 지정한다.
- 문은 인접 일반 타일의 통로와 높이·폭·위치가 맞아야 한다. 표시점 데이터와 실제 구멍/콜라이더도 일치해야 한다.

방 내부 로컬 격자는 남서쪽을 `(0,0)`, 북동쪽을 `(2,2)`로 잡는다.
북쪽 중앙 출입구는 `(1,2), North`, 동쪽 중앙 출입구는 `(2,1), East`로 표현한다.
문은 해당 경계 칸의 외곽 변에 놓는다. 프리팹 피벗과 출입구 로컬 격자 원점은 서로 다른 기준이다.

기존 `MazeTileView`는 1칸 타일의 네 벽을 관리한다.
큰 방 루트에는 `MazeRoomView`, 문 표시점에는 `MazeRoomEntranceMarker`를 붙인다.
표시점은 `Local Cell`과 `Direction`으로 경계를 지정하며, `Align Marker To Grid`로 위치를 맞출 수 있다.

### 논리 데이터와 문 동작

미로 계산에 필요한 방 데이터는 방 식별자, 배치 기준 좌표, 90° 단위 회전값, 점유한 9칸, 회전된 출입구의 경계 칸 좌표와 방향이다.
Unity 쪽이 표시점에서 이 정보를 읽어 계산 코드에 전달한다.
Core·Generation·Validation은 프리팹, Transform, 문 스크립트를 직접 참조하지 않는다.
문 열림 애니메이션, 잠금 조건, 상호작용은 게임/표시 계층에서 담당한다.

초기 설계에서는 방 안의 모든 출입구와 보물 상호작용 지점 사이를 이동할 수 있다고 가정한다.
내부 벽으로 공간을 분리하면 별도 내부 연결 정보가 필요하다.
잠긴 문을 쓰는 경우 검증 대상이 현재 이동 가능성인지, 잠금 해제 후 이동 가능성인지 정하고 상태를 반영해야 한다.

### 미로 생성 순서

1. 시작점과 겹치지 않는 3x3 방 영역들을 먼저 예약하고 출입구 데이터를 정한다.
2. 각 출입구 바깥에 통로를 연결할 공간이 있는지 검사한다.
3. 일반 타일과 방 전체를 각각 연결 단위로 취급해 미로를 생성한다. 방 내부 9칸을 독립 DFS 대상으로 파지 않는다.
4. 지정된 출입구에서만 방과 통로를 연결하고, 인접 일반 타일의 맞은편 벽을 연다.
5. 방 점유 영역 밖의 도달 가능한 가장자리 칸에 기존 출구를 배치한다.
6. BFS로 시작점에서 출구와 모든 보물방까지의 경로를 검사한다.
7. 검증된 결과에 일반 타일 프리팹과 방당 하나의 3x3 프리팹을 배치한다.

DFS는 방 중심 좌표를 대표 방문 노드로 사용하고, 방의 출입구에서 바깥 칸으로 탐색한다.
출입구가 여러 개면 전체 경로에 순환이 생길 수 있으므로, 합쳐진 미로가 반드시 트리 형태일 필요는 없다.
출입구가 없는 외벽에는 연결을 만들지 않는다.

### 경로 검증과 미로 변경

`MazeGrid.AddRoom`이 내부 칸들의 연결을 일관된 격자 데이터로 반영하므로 기존 BFS가 방 안에서도 탐색할 수 있다.
시작점에서 모든 방 중심과 출구에 도달 가능한지 확인한다.
제작된 벽이나 닫힌 출입구를 가상의 통로로 처리해서는 안 된다.

보물방은 9칸 전체와 출입구 연결을 보호한다. 주변 통로는 변경 후에도 보물방까지 도달 가능해야 한다.
방의 3x3 점유 영역과 미로 변경의 3x3 영역은 다른 개념이다.
미로 변경 중 방의 일부만 재생성하지 않는다.
현재 특수방도 초기 배치 후 9칸을 보호한다. 동적 이동·제거를 도입한다면 점유 영역 전체와 외부 연결을 함께 검증해야 한다.

`MazeRegionChanger.TryChange`는 무작위 정사각형 구역을 골라 일반 타일 사이의 내부 벽만 다시 만든다.
`ChangeRegions`는 설정된 개수만큼 서로 겹치지 않는 구역에 이를 반복 적용한다. 각 변경마다 도달성을 검증하며, 공간이 부족하면 가능한 개수까지만 변경한다.
방·출구·플레이어/미라 안전 반경의 벽과 구역 바깥으로 이어지는 벽은 유지한다.
변경 후 현재 플레이어 위치에서 모든 칸으로 이동할 수 있는지 검사하고, 실패하거나 벽이 그대로면 해당 구역을 원상복구한 뒤 다른 구역을 시도한다.
시드를 지정하면 같은 미로와 위치에서 변경 결과를 재현할 수 있다.
`MazeRuntimeChangeController`가 설정된 주기마다 플레이어 월드 위치를 격자 좌표로 변환하고 변경을 시도한다. 성공하면 `MazeRenderer.Refresh`로 벽을 갱신한다.
`MazeRenderer`는 배치 또는 갱신 후 `GeometryChanged` 이벤트만 보낸다. 몬스터 이동용 NavMesh 재구성은 `Assets/Script/Monster/MazeNavMeshUpdater.cs`가 선택적으로 담당하며 미로 코드에 AI 패키지 의존성을 두지 않는다.
미라 Transform은 실행기에서 선택적으로 연결한다. 지정하지 않으면 미라 안전 반경은 적용되지 않는다.

### 아직 정하지 않은 사항

- 보물방·특수방별 최종 출입구 수. 현재는 표시한 문을 모두 연결한다.
- 원본 프리팹 루트는 회전 0, 스케일 1로 제작한다. 배치 시 방마다 90° 단위로 회전한다.
- 특수방의 등장·제거 시점과 보호 정책.
- 미로 크기, 방 개수, 방 사이 간격, 배치 재시도 및 실패 처리.

현재 설정 에셋은 15x15 미로에 보물방 4개와 특수방 1개다.
맵 가장자리를 일반 통로로 남기고 방 사이를 최소 한 칸 띄운다.
배치를 최대 128번 재시도하고 실패하면 크기/개수를 조정하도록 오류를 출력한다.

## 플레이어 시작 위치

`MazeRenderer`가 만든 시작 타일 `(0,0)`의 월드 위치를 `PlayerMazeSpawner`가 사용한다.
`Maze_TestScene`은 씬에 있는 플레이어를 재배치한다. 플레이어 프리팹을 사용할 때는 스포너의 `Existing Player` 참조를 비우고 `Player Prefab`에 연결한다.
`Spawn Height`는 플레이어 피벗과 바닥 높이에 맞춰 조정한다. 플레이어 생성 코드는 미로 계산 코드 밖에 둔다.

## 경로 검증

`MazePathValidator`는 시작 좌표에서 한 목적지(`IsReachable`) 또는 여러 목적지(`AreReachable`)까지 열린 통로로 이동할 수 있는지 검사한다.
초기 생성 후에는 시작 칸에서 출구와 모든 보물방에 도달 가능한지 확인한다.
게임 중 벽 변경은 결과를 확정하기 전에 같은 검증기를 사용한다.

공개용으로 미로 시스템을 분리할 때는 게임 전용 `Treasure Room Prefab` 연결과 보물방 배치를 제외하고, 특수방 리스트와 출구방 연결은 유지한다.

## 다음 구현 추천

1. 제작한 큰 방 프리팹에 루트/출입구 컴포넌트를 붙이고 설정에 연결한다.
2. Unity Play Mode에서 실제 벽·콜라이더·문과 논리 경로가 일치하는지 확인한다.
3. 맵 크기·방 개수·출입구 수를 플레이테스트로 조정한다.
4. 게임 중 변경 주기와 현재 캐릭터 위치를 연결하고, 성공한 변경 결과를 화면에 갱신한다.

런타임 변경 결과는 현재 플레이어 위치에서 출구와 필요한 방에 도달 가능한지 검사하고, 길이 끊기면 취소한다.
