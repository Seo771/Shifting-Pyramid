# 미로 시스템 아키텍처 초안

이 문서는 피라미드 미로 게임의 미로 시스템을 어떻게 나눠서 개발할지 정리한 초안이다.

중요한 방향은 두 가지다.

1. 지금은 게임 완성이 우선이다.
2. 그래도 미로 알고리즘은 나중에 GitHub에 공개할 수 있도록 게임 전용 코드와 분리한다.

즉, 처음부터 거대한 라이브러리처럼 만들지는 않지만, `플레이어`, `미라`, `보물`, `출구`, `UI` 같은 게임 요소가 미로 알고리즘 안으로 들어가지 않게 한다.

추가로 정한 현실적인 기준:

- 이 프로젝트는 나중에 GitHub에 올릴 때도 Unity 프로젝트 형태로 공개할 가능성이 높다.
- 따라서 미로 코드를 완전히 Unity 밖의 독립 라이브러리처럼 과하게 분리하지 않는다.
- 대신 미로 계산 로직과 Unity 표시 로직만 적당히 나눈다.
- `MazeGrid`, `MazeTile`, `MazeGenerator`, `PathValidator`처럼 판단과 계산을 담당하는 코드는 `GameObject`, `MonoBehaviour`, `Transform`에 직접 의존하지 않게 한다.
- `MazeTileView`, `MazeRenderer`, `MazeBootstrapper`처럼 씬에 보여주는 코드는 Unity 전용 스크립트로 만든다.
- 별도 DLL, 과한 인터페이스, 의존성 주입, 에디터 툴, 패키지 구조는 지금 단계에서 만들지 않는다.

정리하면:

```text
Unity 프로젝트 안에서 개발한다.
하지만 미로 계산 코드와 Unity 표시 코드는 섞지 않는다.
```

이 정도 분리는 구현 난이도를 크게 올리지 않으면서도, 나중에 포트폴리오로 공개할 때 구조가 깔끔하게 보이게 해준다.

---

## 1. 전체 구조

추천 폴더 구조:

```text
Assets/
└─ Script/
   ├─ GameManager.cs
   ├─ ExitGate.cs
   │
   └─ Maze/
      ├─ Core/
      ├─ Settings/
      ├─ Generation/
      ├─ Validation/
      ├─ RuntimeChange/
      └─ View/
```

각 폴더의 역할:

```text
Maze/Core
미로의 순수 데이터 구조

Maze/Settings
미로 크기, 방 개수, 변경 주기 같은 밸런싱 설정 에셋

Maze/Generation
처음 미로를 만드는 알고리즘

Maze/Validation
길이 이어져 있는지 검사하는 알고리즘

Maze/RuntimeChange
게임 중 일부 구역의 벽을 바꾸는 로직

Maze/View
미로 데이터를 Unity 오브젝트로 보여주는 코드
```

---

## 2. 가장 중요한 분리 기준

### Maze Core에 들어가야 하는 것

미로 알고리즘으로 봐도 되는 코드다.

예시:

- 타일 좌표
- 타일의 벽 상태
- 타일끼리 연결되어 있는지
- 미로 전체 그리드
- 방향 정보
- 초기 미로 생성
- BFS 경로 검사
- 변경 가능한 구역 선택
- 변경 후보가 유효한지 검사

이 코드는 나중에 다른 게임에서도 쓸 수 있어야 한다.

### Maze Core에 들어가면 안 되는 것

게임 전용 코드다.

예시:

- Player
- Mummy
- Treasure
- Exit
- Trap
- UI
- Sound
- VFX
- Camera Shake
- Unity 씬 오브젝트 직접 제어

미로 알고리즘이 플레이어나 보물을 직접 알면 나중에 GitHub에 공개하기 어렵다.

대신 필요한 정보는 좌표 데이터로 넘긴다.

예시:

```text
나쁜 방식:
MazeGenerator가 TreasureRoom 오브젝트를 직접 찾는다.

좋은 방식:
Game 쪽에서 보물방 좌표 목록을 만들고,
Maze 쪽에는 "이 좌표는 보호해야 한다"라고 넘긴다.
```

---

## 3. 스크립트 분리 계획

### 3.1 Maze/Core

#### MazeCoordinate

역할:

- 미로 안의 좌표를 표현한다.
- 예: `(0, 0)`, `(3, 5)`

가지면 좋은 내용:

```text
int X
int Y
좌표 더하기
같은 좌표인지 비교
```

주의:

- Unity의 `Vector3` 대신 미로 전용 좌표를 쓴다.
- 그래야 미로 알고리즘이 Unity 위치값에 묶이지 않는다.

---

#### MazeDirection

역할:

- 북, 동, 남, 서 방향을 표현한다.

가지면 좋은 내용:

```text
North
East
South
West
반대 방향 구하기
방향을 좌표 이동값으로 바꾸기
```

예시:

```text
North -> (0, 1)
East  -> (1, 0)
South -> (0, -1)
West  -> (-1, 0)
```

---

#### MazeTile

역할:

- 타일 하나의 논리 상태를 저장한다.

가지면 좋은 내용:

```text
MazeCoordinate Coordinate
북쪽 벽 열림 여부
동쪽 벽 열림 여부
남쪽 벽 열림 여부
서쪽 벽 열림 여부
보호 타일 여부
타일 종류
```

주의:

- `GameObject`, `Transform`, `Collider`를 넣지 않는다.
- 보물방, 출구 같은 것은 직접 오브젝트로 들고 있지 않는다.
- 필요하면 타일 종류만 enum으로 표시한다.

---

#### MazeTileType

역할:

- 타일이 어떤 목적의 타일인지 표시한다.

후보:

```text
Normal
TreasureRoom
Exit
FixedSpecialRoom
DynamicSpecialRoom
```

주의:

- 타입은 최소한으로만 둔다.
- 너무 많은 게임 규칙을 여기 넣으면 Maze Core가 게임에 묶인다.

---

#### MazeGrid

역할:

- 미로 전체 타일을 관리한다.
- 좌표로 타일을 찾는다.
- 인접 타일을 찾는다.
- 두 타일 사이 벽 상태를 일관되게 바꾼다.

가지면 좋은 내용:

```text
int Width
int Height
MazeTile GetTile(MazeCoordinate coordinate)
bool Contains(MazeCoordinate coordinate)
bool TryGetNeighbor(...)
void SetConnection(...)
```

중요:

인접한 두 타일은 벽 상태가 항상 맞아야 한다.

예시:

```text
A 타일의 동쪽이 열림
= B 타일의 서쪽도 열림
```

한쪽만 열리는 상태는 버그다.

---

### 3.2 Maze/Settings

#### MazeBalanceSettings

역할:

- 코드 수정 없이 인스펙터에서 밸런싱 값을 조정할 수 있게 한다.
- Unity의 `ScriptableObject`로 만든다.
- 난이도별 프리셋을 만들 수 있게 한다.

처음에 넣을 값:

```text
미로 가로 크기
미로 세로 크기
보물방 개수
고정 특수방 개수
탈출에 필요한 보물 개수
미로 변경 구역 크기
미로 변경 주기
플레이어 안전 반경
미라 안전 반경
랜덤 시드 사용 여부
시드 값
```

사용 방식:

```text
Unity Project 창 우클릭
-> Create
-> Shifting Pyramid
-> Maze Balance Settings

GameManager
-> requiredTreasureCount를 읽는다.

MazeGenerator 또는 MazeBootstrapper
-> mazeWidth, mazeHeight, room count를 읽는다.

MazeChanger
-> change interval, region size, safe radius를 읽는다.
```

주의:

- 처음에는 `MazeBalanceSettings` 하나로 시작한다.
- 설정 값이 너무 많아지면 나중에 `MazeGenerationSettings`, `MazeRuntimeChangeSettings`, `GameRuleSettings`로 나눈다.
- 지금 단계에서는 별도 에디터 툴을 만들지 않는다.

---

### 3.3 Maze/Generation

#### IMazeGenerator

역할:

- 미로 생성기의 공통 형태를 정한다.

예상 형태:

```text
MazeGrid Generate(int width, int height, int seed)
```

주의:

- 처음부터 인터페이스를 너무 많이 만들 필요는 없다.
- 하지만 생성 알고리즘은 나중에 DFS, Prim 등으로 바뀔 수 있어서 이 정도 분리는 괜찮다.

---

#### DepthFirstMazeGenerator

역할:

- DFS 방식으로 기본 연결 미로를 만든다.

처음 구현 목표:

```text
1. 모든 타일을 만든다.
2. 시작 타일부터 DFS로 이동한다.
3. 방문하지 않은 이웃으로 이동할 때 벽을 연다.
4. 모든 타일이 연결된 미로를 만든다.
```

주의:

- 보물방, 출구 배치는 여기서 직접 하지 않는 것이 좋다.
- 일단 순수한 미로만 만든다.
- 게임 전용 배치는 나중에 별도 스크립트에서 처리한다.

---

### 3.4 Maze/Validation

#### PathValidator

역할:

- 특정 좌표에서 다른 좌표까지 갈 수 있는지 검사한다.
- BFS로 충분하다.

필요한 기능:

```text
bool HasPath(MazeGrid grid, MazeCoordinate start, MazeCoordinate goal)
bool CanReachAll(MazeGrid grid, MazeCoordinate start, List<MazeCoordinate> goals)
HashSet<MazeCoordinate> GetReachableTiles(...)
```

사용 예시:

```text
플레이어 위치 -> 출구 도달 가능?
플레이어 위치 -> 남은 보물방 전부 도달 가능?
미라가 완전히 고립되지는 않았는가?
```

주의:

- `Player` 오브젝트를 직접 받지 않는다.
- `TreasureRoom` 오브젝트를 직접 받지 않는다.
- 좌표만 받는다.

---

### 3.5 Maze/RuntimeChange

#### MazeChangeRequest

역할:

- 미로 변경을 시도할 때 필요한 조건을 담는다.

들어갈 수 있는 정보:

```text
플레이어 현재 좌표
도달 가능해야 하는 목표 좌표 목록
변경하면 안 되는 보호 좌표 목록
```

예시:

```text
ProtectedCoordinates:
- 플레이어 주변
- 보물방
- 출구
- 고정 특수방
- 미라 주변
```

---

#### MazeRegion

역할:

- 변경할 구역을 표현한다.

처음 후보:

```text
3x3 구역
```

가지면 좋은 내용:

```text
시작 좌표
너비
높이
구역 안 좌표 목록
```

---

#### MazeRegionSelector

역할:

- 미로에서 변경 가능한 구역을 고른다.

조건:

```text
보호 좌표와 겹치지 않을 것
맵 범위를 벗어나지 않을 것
너무 플레이어 근처는 피할 것
```

---

#### MazeChanger

역할:

- 실제 런타임 미로 변경 흐름을 담당한다.

흐름:

```text
1. 변경할 구역 선택
2. 변경 전 상태 저장
3. 구역 내부 벽 상태 변경
4. 바깥과 연결되는 통로 정리
5. PathValidator로 검증
6. 유효하면 적용
7. 실패하면 롤백
```

주의:

- 처음부터 완벽하게 만들 필요는 없다.
- 1차 목표는 3x3 한 구역만 바꾸는 것이다.
- 여러 구역 동시 변경은 나중에 한다.

---

### 3.6 Maze/View

이 폴더는 Unity 오브젝트를 다룬다.

Maze Core와 달리 여기서는 `GameObject`, `Transform`, `MonoBehaviour`를 사용해도 된다.

#### MazeTileView

역할:

- 타일 하나의 3D 오브젝트를 관리한다.

Unity 오브젝트 구조:

```text
MazeTileObject
├─ Floor
├─ Pillars
├─ Wall_North
├─ Wall_East
├─ Wall_South
└─ Wall_West
```

해야 할 일:

```text
MazeTile 데이터를 읽는다.
벽이 있으면 해당 벽 오브젝트를 켠다.
벽이 없으면 해당 벽 오브젝트를 끈다.
```

주의:

- 벽 오브젝트 표시만 담당한다.
- 미로 생성 알고리즘은 넣지 않는다.

---

#### MazeRenderer

역할:

- `MazeGrid` 전체를 씬에 보여준다.

해야 할 일:

```text
타일 프리팹 생성
좌표에 맞게 배치
각 타일의 벽 상태 반영
미로 변경 후 벽 상태 다시 반영
```

---

#### MazeBootstrapper

역할:

- 테스트용으로 씬 시작 시 미로를 생성하고 렌더링한다.

처음에는 개발 편의용이다.

해야 할 일:

```text
맵 크기 입력
시드 입력
DepthFirstMazeGenerator 호출
MazeRenderer에 전달
```

주의:

- 나중에 게임 시작 흐름이 정리되면 GameManager나 StageManager 쪽에서 호출하게 바꿀 수 있다.

---

## 4. 게임 코드와 연결하는 방식

게임 쪽은 미로 시스템에 오브젝트를 넘기지 않는다.

대신 좌표를 넘긴다.

예시:

```text
PlayerController
-> 현재 플레이어가 있는 MazeCoordinate 제공

TreasureRoom
-> 자신의 MazeCoordinate 제공

ExitGate
-> 자신의 MazeCoordinate 제공

GameManager 또는 StageManager
-> 보호 좌표 목록을 MazeChanger에 전달
```

이렇게 하면 미로 알고리즘은 게임 오브젝트를 몰라도 된다.

---

## 5. 1차 구현 순서

### 1단계: 순수 미로 데이터

구현 파일:

```text
MazeCoordinate
MazeDirection
MazeTile
MazeTileType
MazeGrid
```

목표:

```text
좌표 기반 타일 생성
벽 열기/닫기
인접 타일 벽 상태 동기화
```

---

### 2단계: 초기 미로 생성

시작 전에 설정 에셋을 만든다.

구현 파일:

```text
MazeBalanceSettings
```

목표:

```text
미로 크기와 방 개수를 인스펙터에서 조정 가능하게 만들기
GameManager가 필요한 보물 수를 설정 에셋에서 읽게 하기
```

---

### 3단계: 초기 미로 생성

구현 파일:

```text
IMazeGenerator
DepthFirstMazeGenerator
```

목표:

```text
맵 전체가 연결된 기본 미로 생성
```

---

### 4단계: Unity 표시

구현 파일:

```text
MazeTileView
MazeRenderer
MazeBootstrapper
```

목표:

```text
생성된 미로를 3D 타일과 벽으로 표시
```

---

### 5단계: 경로 검증

구현 파일:

```text
PathValidator
```

목표:

```text
시작 위치에서 출구까지 갈 수 있는지 검사
시작 위치에서 보물방까지 갈 수 있는지 검사
```

---

### 6단계: 런타임 미로 변경

구현 파일:

```text
MazeChangeRequest
MazeRegion
MazeRegionSelector
MazeChanger
```

목표:

```text
보호 좌표를 피해서 3x3 구역 선택
구역 내부 벽 변경
변경 후에도 주요 좌표에 도달 가능한지 검사
유효하면 적용, 아니면 취소
```

---

## 6. 당장 만들 첫 스크립트 추천

가장 먼저 만들 파일:

```text
Assets/Script/Maze/Core/MazeCoordinate.cs
Assets/Script/Maze/Core/MazeDirection.cs
Assets/Script/Maze/Core/MazeTile.cs
Assets/Script/Maze/Core/MazeGrid.cs
```

이 네 개가 생기면 이후 생성기, 검증기, 뷰가 전부 이 데이터 위에 올라갈 수 있다.

처음부터 `MonoBehaviour`를 만들지 말고, 순수 C# 클래스부터 만드는 것이 좋다.

이유:

```text
1. 테스트하기 쉽다.
2. 미로 알고리즘이 Unity 씬에 묶이지 않는다.
3. 나중에 GitHub에 공개하기 쉽다.
```

---

## 7. 네이밍 규칙

추천:

```text
MazeCoordinate
MazeDirection
MazeTile
MazeGrid
MazeGenerator
PathValidator
MazeChanger
MazeTileView
MazeRenderer
```

피하고 싶은 이름:

```text
PyramidMazeTile
TreasureMazeGrid
MummyPathMaze
ExitMazeGenerator
```

이런 이름은 미로 코드가 피라미드 게임에 너무 묶여 보인다.

---

## 8. 나중에 GitHub 공개할 때 남길 수 있는 부분

공개 후보:

```text
Maze/Core
Maze/Generation
Maze/Validation
Maze/RuntimeChange 일부
```

공개에서 제외할 부분:

```text
Maze/View
GameManager
ExitGate
Player
Mummy
Treasure
Trap
UI
```

`Maze/View`는 Unity 전용 표시 코드라서 샘플로는 넣을 수 있지만, 핵심 알고리즘 패키지와는 분리하는 편이 좋다.

---

## 9. 현재 결론

처음 구현은 작게 간다.

1. 순수 미로 데이터 구조를 만든다.
2. DFS로 기본 미로를 만든다.
3. Unity 타일 프리팹으로 보여준다.
4. BFS로 경로 검증을 붙인다.
5. 3x3 구역 변경을 붙인다.

이 순서로 가면 게임 완성에도 도움이 되고, 나중에 미로 알고리즘만 떼어서 GitHub에 올리기도 쉽다.
