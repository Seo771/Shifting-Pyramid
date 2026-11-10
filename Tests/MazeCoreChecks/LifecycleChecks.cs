using System;
using System.Collections.Generic;
using System.Linq;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Generation;
using ShiftingPyramid.Maze.RuntimeChange;
using ShiftingPyramid.Maze.View;

internal static class LifecycleChecks
{
    public static void Run()
    {
        VerifyChanges();
        VerifyPipeline();
        VerifyAdditionalProtection();
        Console.WriteLine("PASS: full/partial changes, ordered post-processing, failure/reentry guards and multiple protected actors.");
    }

    private static void VerifyChanges()
    {
        var grid = new MazeGrid(7, 7);
        var before = grid.Clone();
        Assert(MazeChangeSet.Full(grid).IsFullRebuild && MazeChangeSet.Full(grid).Cells.Count == 49,
            "Full rebuild includes all cells.");
        Assert(MazeChangeSet.Between(before, grid).Cells.Count == 0, "No-op has no delta.");
        grid.SetConnection(MazeCoordinate.Zero, MazeDirection.East, true);
        var changes = MazeChangeSet.Between(before, grid);
        Assert(!changes.IsFullRebuild && changes.Cells.Count == 2
            && changes.Cells.Contains(MazeCoordinate.Zero)
            && changes.Cells.Contains(new MazeCoordinate(1, 0)), "Shared walls report both sides.");

        var template = new MazeRoomTemplate(MazeTileType.SpecialRoom,
            new[] { new MazeRoomEntrance(new MazeCoordinate(1, 2), MazeDirection.North) });
        grid.AddRoom(new MazeRoom(new MazeCoordinate(2, 2), template, 0));
        before = grid.Clone();
        Assert(MazeChangeSet.Between(before, grid).Cells.Count == 0, "Cloning keeps existing room identity.");
        grid.RemoveRoom(grid.Rooms[0]);
        grid.AddRoom(new MazeRoom(new MazeCoordinate(2, 2), template, 1, 1));
        Assert(MazeChangeSet.Between(before, grid).Cells.Count == 9,
            "Room template/rotation replacement reports the entire footprint.");

        var live = new MazeGrid(4, 4);
        var identity = live;
        var plan = live.Clone();
        plan.SetConnection(MazeCoordinate.Zero, MazeDirection.North, true);
        Assert(!live.GetTile(MazeCoordinate.Zero).IsOpen(MazeDirection.North),
            "Planning cannot mutate the active maze.");
        live.CopyFrom(plan);
        Assert(ReferenceEquals(live, identity) && live.GetTile(MazeCoordinate.Zero).IsOpen(MazeDirection.North),
            "Committing a validated plan keeps grid identity for subscribers.");
    }

    private static void VerifyPipeline()
    {
        var grid = new MazeGrid(2, 2);
        var changes = MazeChangeSet.Full(grid);
        var pipeline = new MazeUpdatePipeline();
        var stages = new List<string>();
        // Subscribe in reverse order: execution must still follow domain phases.
        pipeline.NavigationUpdating += (_, __) => stages.Add("navigation");
        pipeline.DecorationsUpdating += (_, __) => stages.Add("torches");
        pipeline.ContentsUpdating += (_, __) =>
        {
            Assert(pipeline.IsUpdating, "Not ready while contents are being placed.");
            var rejected = false;
            try { pipeline.Apply(grid, changes); }
            catch (InvalidOperationException) { rejected = true; }
            Assert(rejected, "Reentrant rebuild must not clear active content.");
            stages.Add("boxes");
        };
        pipeline.Preparing += (_, __) => stages.Add("cleanup");
        pipeline.Apply(grid, changes);
        Assert(stages.SequenceEqual(new[] { "cleanup", "boxes", "torches", "navigation" }) && !pipeline.IsUpdating,
            "Content and lighting finish before navigation.");

        var failing = new MazeUpdatePipeline();
        var navigationRan = false;
        Action<MazeGrid, MazeChangeSet> fail = (_, __) => throw new InvalidOperationException("test failure");
        failing.ContentsUpdating += fail;
        failing.NavigationUpdating += (_, __) => navigationRan = true;
        try { failing.Apply(grid, changes); }
        catch (InvalidOperationException) { }
        Assert(!failing.IsUpdating && !navigationRan, "Failed placement aborts later phases and releases the guard.");
        failing.ContentsUpdating -= fail;
        failing.Apply(grid, changes);
        Assert(navigationRan, "A later rebuild can recover after failure.");
    }

    private static void VerifyAdditionalProtection()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var grid = new DepthFirstMazeGenerator().Generate(20, 20, seed);
            var areas = new[]
            {
                new MazeProtectedArea(new MazeCoordinate(8, 8), 1),
                new MazeProtectedArea(new MazeCoordinate(15, 14), 2)
            };
            var before = grid.Clone();
            new MazeRegionChanger().ChangeRegions(grid, 5, 3, MazeCoordinate.Zero, 1,
                null, 1, seed, areas);
            foreach (var cell in MazeChangeSet.Between(before, grid).Cells)
                Assert(!MazeProtectedArea.Contains(areas, cell), "Every actor keeps its surrounding walls.");
        }

        var template = new MazeRoomTemplate(MazeTileType.SpecialRoom,
            new[] { new MazeRoomEntrance(new MazeCoordinate(1, 2), MazeDirection.North) });
        var roomGrid = new MazeGrid(15, 15);
        new MazeRoomPlacer().Place(roomGrid, new[] { template }, 4);
        new DepthFirstMazeGenerator().GenerateInto(roomGrid, 4);
        var room = roomGrid.Rooms[0];
        var roomSnapshot = roomGrid.Clone();
        var replaced = new SpecialRoomReplacer().TryReplace(roomGrid, new[] { template },
            MazeCoordinate.Zero, 1, null, 1, out _, out _, 4,
            new[] { new MazeProtectedArea(room.Center, 0) });
        Assert(!replaced && MazeChangeSet.Between(roomSnapshot, roomGrid).Cells.Count == 0,
            "A special room containing any protected actor cannot disappear.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
