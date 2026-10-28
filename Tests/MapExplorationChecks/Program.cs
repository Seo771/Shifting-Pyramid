using System;
using System.Linq;
using ShiftingPyramid.Game.Map;
using ShiftingPyramid.Maze.Core;

internal static class Program
{
    private static void Main()
    {
        var grid = new MazeGrid(7, 7);
        var start = MazeCoordinate.Zero;
        var east = new MazeCoordinate(1, 0);
        var state = new MapExplorationState(grid);
        var notified = 0;
        state.TileChanged += _ => notified++;

        Assert(state.Width == 7 && state.Height == 7, "Grid size is retained.");
        Assert(!state.TryGetKnownTile(start, out _), "Unexplored structure stays hidden.");
        Assert(!state.Reveal(new MazeCoordinate(-1, 0)), "Outside cells cannot be revealed.");
        Assert(state.Reveal(start) && !state.Reveal(start), "Reveal occurs once per cell.");
        Assert(state.Reveal(east), "A second cell can be revealed.");
        Assert(notified == 2, "Only new discoveries notify the map view.");
        Assert(state.Reconcile(grid).Count == 0, "Unchanged grid keeps exploration.");
        Assert(notified == 2, "Unchanged geometry does not refresh map tiles.");

        grid.SetConnection(start, MazeDirection.East, true);
        var changed = state.Reconcile(grid);
        Assert(changed.Count == 2 && changed.Contains(start) && changed.Contains(east),
            "A shared wall invalidates both cells.");
        Assert(!state.IsExplored(start) && !state.IsExplored(east), "Changed cells are hidden.");
        Assert(notified == 4, "Only changed cells notify the map view.");
        Assert(!state.TryGetKnownTile(start, out _), "Invalidated structure stays hidden.");
        Assert(state.Reconcile(grid).Count == 0, "Repeated reconciliation is stable.");
        Assert(state.Reveal(start) && state.TryGetKnownTile(start, out var known)
            && known.IsOpen(MazeDirection.East), "Revisit reveals the new wall state.");

        grid.GetTile(start).IsProtected = true;
        Assert(state.Reconcile(grid).Count == 0 && state.IsExplored(start),
            "Protection metadata does not change map geometry.");
        grid.GetTile(start).TileType = MazeTileType.Exit;
        Assert(state.Reconcile(grid).SequenceEqual(new[] { start }) && !state.IsExplored(start),
            "Changing tile type invalidates only that cell.");
        Assert(state.Reveal(start), "A changed tile can be discovered again.");
        Assert(state.Reconcile(new MazeGrid(7, 7)).SequenceEqual(new[] { start, east }),
            "A structural rescan reports only the changed cells.");
        Assert(!new MapExplorationState(new MazeGrid(7, 7)).IsExplored(start),
            "A new maze starts with no explored cells.");
        ExpectException<ArgumentException>(() => state.Reconcile(new MazeGrid(8, 7)));

        var roomCell = new MazeCoordinate(3, 3);
        var first = RoomGrid(1, 0, new MazeCoordinate(2, 2));
        var rooms = new MapExplorationState(first);
        Assert(rooms.Reveal(roomCell), "One room cell can be discovered alone.");
        Assert(!rooms.IsExplored(new MazeCoordinate(2, 3)),
            "Discovering a room cell does not reveal its footprint.");

        var templateChanged = RoomGrid(2, 0, new MazeCoordinate(2, 2));
        Assert(rooms.Reconcile(templateChanged).Contains(roomCell) && !rooms.IsExplored(roomCell),
            "Room template changes invalidate the visited cell.");
        Assert(rooms.Reveal(roomCell), "Updated room can be revisited.");

        var rotated = RoomGrid(2, 1, new MazeCoordinate(2, 2));
        Assert(rooms.Reconcile(rotated).Contains(roomCell) && !rooms.IsExplored(roomCell),
            "Room rotation changes invalidate the visited cell.");
        Assert(rooms.Reveal(roomCell), "Rotated room can be revisited.");

        var moved = RoomGrid(2, 1, new MazeCoordinate(3, 2));
        Assert(rooms.Reconcile(moved).Contains(roomCell) && !rooms.IsExplored(roomCell),
            "A new room origin invalidates overlapping discovered cells.");

        Console.WriteLine("PASS: map exploration, hidden tiles, wall/type/room invalidation, revisits and reset boundaries.");
    }

    private static MazeGrid RoomGrid(int templateIndex, int rotation, MazeCoordinate origin)
    {
        var grid = new MazeGrid(8, 7);
        var template = new MazeRoomTemplate(MazeTileType.SpecialRoom,
            new[] { new MazeRoomEntrance(new MazeCoordinate(1, 2), MazeDirection.North) });
        grid.AddRoom(new MazeRoom(origin, template, templateIndex, rotation));
        return grid;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void ExpectException<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
