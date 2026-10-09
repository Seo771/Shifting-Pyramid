using System;
using System.Collections.Generic;
using System.Linq;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Generation;
using ShiftingPyramid.Maze.RuntimeChange;
using ShiftingPyramid.Maze.Validation;

internal static class Program
{
    private static readonly MazeDirection[] Directions = (MazeDirection[])Enum.GetValues(typeof(MazeDirection));

    private static void Main()
    {
        VerifyRotations();
        var observedRotations = new HashSet<int>();
        for (var seed = 0; seed < 250; seed++)
        {
            var first = Generate(seed);
            var second = Generate(seed);
            Assert(Signature(first) == Signature(second), "Seed must reproduce room placement and walls.");
            Verify(first);
            foreach (var placedRoom in first.Rooms) observedRotations.Add(placedRoom.RotationQuarterTurns);
        }
        Assert(observedRotations.SetEquals(new[] { 0, 1, 2, 3 }), "Placement must use all quarter turns.");

        // 기존 방 없는 DFS에서도 완전 연결과 내부 통로 수를 보장한다.
        var plain = new DepthFirstMazeGenerator().Generate(8, 6, 12345);
        Assert(new MazePathValidator().AreReachable(plain, MazeCoordinate.Zero,
            plain.Tiles.Select(tile => tile.Coordinate)), "Plain DFS reachability.");
        var edgeCount = plain.Tiles.Sum(tile => Directions.Count(direction =>
            tile.IsOpen(direction) && plain.Contains(tile.Coordinate + direction.ToOffset()))) / 2;
        Assert(edgeCount == 8 * 6 - 1, "Plain DFS should remain a tree.");

        var tiny = new MazeGrid(5, 5);
        ExpectFailure<InvalidOperationException>(() => new MazeRoomPlacer().Place(tiny,
            new[] { Template(MazeTileType.TreasureRoom, 0), Template(MazeTileType.SpecialRoom, 1) }, 1));
        Assert(tiny.Rooms.Count == 0 && tiny.Tiles.All(tile => tile.TileType == MazeTileType.Normal),
            "Failed placement must leave the grid unchanged.");

        ExpectFailure<ArgumentException>(() => new MazeRoomEntrance(new MazeCoordinate(1, 1), MazeDirection.North));
        ExpectFailure<ArgumentException>(() => new MazeRoomTemplate(MazeTileType.TreasureRoom,
            Array.Empty<MazeRoomEntrance>()));
        var duplicate = new MazeRoomEntrance(new MazeCoordinate(1, 2), MazeDirection.North);
        ExpectFailure<ArgumentException>(() => new MazeRoomTemplate(MazeTileType.SpecialRoom, new[] { duplicate, duplicate }));

        // 유일한 입구를 닫으면 방 내부와 외부 사이를 양방향으로 이동할 수 없다.
        var isolated = new MazeGrid(7, 7);
        new MazeRoomPlacer().Place(isolated, new[] { Template(MazeTileType.TreasureRoom, 0) }, 3);
        new DepthFirstMazeGenerator().GenerateInto(isolated, 3);
        var room = isolated.Rooms[0];
        var entrance = room.Entrances[0];
        var cell = room.Origin + entrance.LocalCell;
        isolated.SetConnection(cell, entrance.Direction, false);
        Assert(!new MazePathValidator().IsReachable(isolated, MazeCoordinate.Zero, room.Center), "Closed entrance blocks entry.");
        Assert(!new MazePathValidator().IsReachable(isolated, room.Center, MazeCoordinate.Zero), "Closed entrance blocks exit.");
        ExpectFailure<InvalidOperationException>(() => isolated.SetConnection(room.Origin, MazeDirection.West, true));
        Assert(!new MazePathValidator().IsReachable(isolated, MazeCoordinate.Zero, new MazeCoordinate(-1, 0)), "Out-of-bounds target.");

        var changedCount = 0;
        for (var seed = 0; seed < 100; seed++)
        {
            var first = Generate(seed);
            var second = Generate(seed);
            var before = Signature(first);
            var mummy = new MazeCoordinate(14, 14);
            var changer = new MazeRegionChanger();
            var changed = changer.TryChange(first, 3, MazeCoordinate.Zero, 1, mummy, 1,
                out var origin, seed);
            var repeated = changer.TryChange(second, 3, MazeCoordinate.Zero, 1, mummy, 1,
                out var repeatedOrigin, seed);
            Assert(changed == repeated && origin.Equals(repeatedOrigin)
                && Signature(first) == Signature(second), "Region change must be deterministic.");
            if (!changed)
            {
                Assert(Signature(first) == before, "Failed change must restore every wall.");
                continue;
            }

            changedCount++;
            Assert(Signature(first) != before, "Successful change must alter a wall.");
            Assert(new MazePathValidator().AreReachable(first, MazeCoordinate.Zero,
                first.Tiles.Select(tile => tile.Coordinate)), "Changed maze must stay reachable.");
            var original = Generate(seed);
            foreach (var tile in first.Tiles)
            {
                var coordinate = tile.Coordinate;
                var outside = coordinate.X < origin.X || coordinate.X >= origin.X + 3
                    || coordinate.Y < origin.Y || coordinate.Y >= origin.Y + 3;
                var playerSafe = coordinate.X <= 1 && coordinate.Y <= 1;
                var mummySafe = coordinate.X >= 13 && coordinate.Y >= 13;
                if (outside || playerSafe || mummySafe || tile.IsProtected)
                    foreach (var direction in Directions)
                        Assert(tile.IsOpen(direction) == original.GetTile(coordinate).IsOpen(direction),
                            "Protected, safe, and out-of-region walls must stay unchanged.");
            }
            Verify(first);
        }
        Assert(changedCount > 0, "Default 3x3 region must produce a real change.");

        var disconnected = new MazeGrid(5, 5);
        var disconnectedBefore = Signature(disconnected);
        Assert(!new MazeRegionChanger().TryChange(disconnected, 3, MazeCoordinate.Zero, 0,
            null, 0, out _, 7) && Signature(disconnected) == disconnectedBefore,
            "Unreachable change must be rolled back.");
        ExpectFailure<ArgumentOutOfRangeException>(() => new MazeRegionChanger().TryChange(
            disconnected, 2, MazeCoordinate.Zero, 0, null, 0, out _));

        Console.WriteLine($"PASS: four room rotations, 250 generation seeds, {changedCount}/100 runtime changes, protection, reachability, determinism, and rollback checks.");
    }

    private static void VerifyRotations()
    {
        var source = new MazeRoomTemplate(MazeTileType.TreasureRoom,
            new[] { new MazeRoomEntrance(new MazeCoordinate(0, 2), MazeDirection.North) });
        var cells = new[]
        {
            new MazeCoordinate(0, 2), new MazeCoordinate(2, 2),
            new MazeCoordinate(2, 0), new MazeCoordinate(0, 0)
        };
        var origin = new MazeCoordinate(1, 1);
        for (var quarterTurns = 0; quarterTurns < 4; quarterTurns++)
        {
            var room = new MazeRoom(origin, source, 0, quarterTurns);
            var entrance = room.Entrances.Single();
            Assert(entrance.LocalCell.Equals(cells[quarterTurns])
                && entrance.Direction == Directions[quarterTurns]
                && room.HasEntrance(origin + cells[quarterTurns], Directions[quarterTurns]),
                "Room entrance must rotate with its direction.");
            var grid = new MazeGrid(5, 5);
            grid.AddRoom(room);
            grid.SetConnection(origin + entrance.LocalCell, entrance.Direction, true);
            Assert(grid.GetTile(origin + entrance.LocalCell).IsOpen(entrance.Direction),
                "Rotated entrance must connect to its outside corridor.");
        }
        Assert(source.Entrances[0].LocalCell.Equals(cells[0])
            && source.Entrances[0].Direction == MazeDirection.North,
            "Rotating a room must not mutate its prefab template.");
        ExpectFailure<ArgumentOutOfRangeException>(() => new MazeRoom(origin, source, 0, 4));
    }

    private static MazeGrid Generate(int seed)
    {
        var grid = new MazeGrid(15, 15);
        var templates = Enumerable.Range(0, 7).Select(i =>
            Template(i < 5 ? MazeTileType.TreasureRoom : MazeTileType.SpecialRoom, i % 3)).ToArray();
        new MazeRoomPlacer().Place(grid, templates, seed);
        new DepthFirstMazeGenerator().GenerateInto(grid, seed);
        new MazeExitPlacer().Place(grid);
        return grid;
    }

    private static MazeRoomTemplate Template(MazeTileType type, int variant)
    {
        var entrances = new List<MazeRoomEntrance>
        {
            new MazeRoomEntrance(new MazeCoordinate(1, 2), MazeDirection.North)
        };
        if (variant > 0) entrances.Add(new MazeRoomEntrance(new MazeCoordinate(2, 1), MazeDirection.East));
        if (variant > 1) entrances.Add(new MazeRoomEntrance(new MazeCoordinate(0, 0), MazeDirection.South));
        return new MazeRoomTemplate(type, entrances);
    }

    private static void Verify(MazeGrid grid)
    {
        Assert(grid.Rooms.Count == 7, "Expected seven room prefabs.");
        Assert(grid.Tiles.Count(tile => grid.TryGetRoom(tile.Coordinate, out _)) == 63, "Exactly nine cells per room.");
        Assert(!grid.TryGetRoom(MazeCoordinate.Zero, out _), "Start must be a corridor.");
        var validator = new MazePathValidator();
        var allCells = grid.Tiles.Select(tile => tile.Coordinate).ToArray();
        Assert(validator.AreReachable(grid, MazeCoordinate.Zero, allCells), "All cells must be reachable.");

        foreach (var room in grid.Rooms)
        {
            Assert(room.Cells.All(cell => grid.GetTile(cell).IsProtected
                && grid.GetTile(cell).TileType == room.Template.TileType), "Whole footprint is protected and typed.");
            Assert(validator.AreReachable(grid, room.Center, allCells), "BFS must also start inside a room.");
            foreach (var cell in room.Cells)
                foreach (var direction in Directions)
                    if (!room.Contains(cell + direction.ToOffset()))
                        Assert(grid.GetTile(cell).IsOpen(direction) == room.HasEntrance(cell, direction),
                            "Only authored doors may open; every authored door must open.");
        }

        foreach (var tile in grid.Tiles)
            foreach (var direction in Directions)
                if (grid.TryGetNeighbor(tile.Coordinate, direction, out var neighbor))
                    Assert(tile.IsOpen(direction) == neighbor.IsOpen(direction.Opposite()), "Shared walls must agree.");

        var exit = grid.Tiles.Single(tile => tile.TileType == MazeTileType.Exit);
        Assert(!grid.TryGetRoom(exit.Coordinate, out _) && exit.IsProtected, "Exit must be outside rooms.");
        Assert(exit.Coordinate.X == 0 || exit.Coordinate.Y == 0
            || exit.Coordinate.X == grid.Width - 1 || exit.Coordinate.Y == grid.Height - 1, "Exit must be on edge.");
        Assert(Directions.Count(direction => exit.IsOpen(direction)
            && !grid.Contains(exit.Coordinate + direction.ToOffset())) == 1, "Exit opens one external wall.");
    }

    private static string Signature(MazeGrid grid)
    {
        return string.Join(";", grid.Rooms.Select(room =>
            $"{room.Origin}:{room.RotationQuarterTurns}")) + "|"
            + string.Join(";", grid.Tiles.Select(tile =>
                $"{tile.Coordinate}:{tile.TileType}:{string.Join(",", Directions.Select(tile.IsOpen))}"));
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
