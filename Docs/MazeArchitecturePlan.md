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
Assets/Script/Maze/Settings/MazeBalanceSettings.cs
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

## 방 데이터 범위

현재 방 데이터는 단순하게 유지한다.

```text
보물방
-> 형태는 1개
-> 별도 데이터베이스를 만들지 않는다.
-> 개수는 MazeBalanceSettings.TreasureRoomCount로 관리한다.

특수방
-> 전부 동적으로 등장한다.
-> 아직 종류별 데이터베이스를 만들지 않는다.
-> 개수는 MazeBalanceSettings.SpecialRoomCount로 관리한다.
-> 타일 타입은 MazeTileType.SpecialRoom 하나만 사용한다.
```

나중에 특수방 종류가 여러 개로 확정되고, 각 방마다 프리팹/가중치/등장 조건이 필요해지면 그때 `SpecialRoomData`와 `SpecialRoomDatabase`를 다시 추가한다.

## 다음 구현 추천

다음은 `MazeTile`을 만드는 것이 좋다.

이유:

```text
MazeCoordinate와 MazeDirection이 준비됨
이제 타일 하나가 어떤 벽을 가지고 있는지 표현할 수 있음
그 다음에 MazeGrid로 전체 미로를 만들 수 있음
```

추천 순서:

```text
1. MazeTile
2. MazeGrid
3. DepthFirstMazeGenerator
4. PathValidator
5. MazeTileView / MazeRenderer
```
