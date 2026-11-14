namespace KiranaHub.WarehouseIoT.Pathfinding;

// [KYA KARTA HAI]: Dark-Store warehouse ke liye 2D grid floor map aur A* (A-Star) AGV Pathfinding algorithm implement karta hai.
// [KYU ZAROORI HAI]: Autonomous Guided Vehicles (AGVs) aur smart picker robots ke liye shortest collision-free route calculate karna jo storage shelves aur obstacles ke around navigate kare.
// [KAISE CHALTA HAI]: Manhattan distance heuristic ($h(n) = |x_1 - x_2| + |y_1 - y_2|$) aur cost function ($f(n) = g(n) + h(n)$) use karta hai; PriorityQueue ke zariye open set explore karke optimal path reconstruct karta hai.
// [OUTPUT / SIDE EFFECT]: Collision-free list of coordinates, travel distance, and ASCII visual floor map.
// [VARIATIONS TO TRY]: Diagonal movement allow karein with Euclidean heuristic ($\sqrt{\Delta x^2 + \Delta y^2}$).
public class AStarWarehouseRouter
{
    // [KYA KARTA HAI]: 2D warehouse floor grid array (true = walkable floor, false = shelf/obstacle).
    // [KYU ZAROORI HAI]: Physical warehouse layout represent karna.
    // [KAISE CHALTA HAI]: bool[,] 2D array.
    // [OUTPUT / SIDE EFFECT]: Grid map.
    // [VARIATIONS TO TRY]: Dynamic obstacle avoidance (e.g. another robot blocking the aisle).
    private readonly bool[,] _grid;

    // [KYA KARTA HAI]: Grid width (X dimensions).
    // [KYU ZAROORI HAI]: Boundary checking.
    // [KAISE CHALTA HAI]: Array dimension 0.
    // [OUTPUT / SIDE EFFECT]: Width integer.
    // [VARIATIONS TO TRY]: Dynamic resizing.
    public int Width { get; }

    // [KYA KARTA HAI]: Grid height (Y dimensions).
    // [KYU ZAROORI HAI]: Boundary checking.
    // [KAISE CHALTA HAI]: Array dimension 1.
    // [OUTPUT / SIDE EFFECT]: Height integer.
    // [VARIATIONS TO TRY]: 3D multi-tier mezzanine floor support.
    public int Height { get; }

    // [KYA KARTA HAI]: AStarWarehouseRouter instance initialize karta hai.
    // [KYU ZAROORI HAI]: Warehouse dimensions aur obstacle grid setup karna.
    // [KAISE CHALTA HAI]: Width aur height ke sath empty grid instantiate karta hai (all walkable by default).
    // [OUTPUT / SIDE EFFECT]: Initialized router.
    // [VARIATIONS TO TRY]: ASCII string layout se initialize karein.
    public AStarWarehouseRouter(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        _grid = new bool[width, height];

        // [KYA KARTA HAI]: Sabhi cells ko initially walkable mark karta hai.
        // [KYU ZAROORI HAI]: Clean baseline grid.
        // [KAISE CHALTA HAI]: Nested loops setting true.
        // [OUTPUT / SIDE EFFECT]: Walkable floor.
        // [VARIATIONS TO TRY]: Sparse matrix for huge warehouses.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _grid[x, y] = true;
            }
        }
    }

    // [KYA KARTA HAI]: Warehouse me shelf ya static obstacle place karta hai.
    // [KYU ZAROORI HAI]: Storage racks aur cooling units mark karna jahan se robot nahi guzar sakta.
    // [KAISE CHALTA HAI]: _grid[x, y] = false set karta hai.
    // [OUTPUT / SIDE EFFECT]: Cell becomes non-walkable.
    // [VARIATIONS TO TRY]: Block rectangular aisle ranges.
    public void SetObstacle(int x, int y)
    {
        if (IsValid(x, y))
        {
            _grid[x, y] = false;
        }
    }

    // [KYA KARTA HAI]: Coordinate grid boundaries ke andar hai ya nahi verify karta hai.
    // [KYU ZAROORI HAI]: IndexOutOfRangeException prevent karna.
    // [KAISE CHALTA HAI]: x >= 0 && x < Width && y >= 0 && y < Height.
    // [OUTPUT / SIDE EFFECT]: Boolean bounds indicator.
    // [VARIATIONS TO TRY]: Coordinate wrapping for cyclic toroids.
    public bool IsValid(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    // [KYA KARTA HAI]: Cell walkable floor hai ya nahi check karta hai.
    // [KYU ZAROORI HAI]: Pathfinding collision detection.
    // [KAISE CHALTA HAI]: IsValid && _grid[x, y].
    // [OUTPUT / SIDE EFFECT]: True if robot can traverse cell.
    // [VARIATIONS TO TRY]: Speed cost penalty for rough surfaces.
    public bool IsWalkable(int x, int y) => IsValid(x, y) && _grid[x, y];

    // [KYA KARTA HAI]: Do points ke beech Manhattan distance heuristic calculate karta hai.
    // [KYU ZAROORI HAI]: A* search algorithm ke liye admissible aur consistent heuristic provide karna.
    // [KAISE CHALTA HAI]: $|x_1 - x_2| + |y_1 - y_2|$.
    // [OUTPUT / SIDE EFFECT]: Integer distance score.
    // [VARIATIONS TO TRY]: Octile distance for 8-directional movement.
    public static int ManhattanDistance(GridPoint a, GridPoint b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    // [KYA KARTA HAI]: Start point se Goal point tak optimal collision-free path find karta hai using A*.
    // [KYU ZAROORI HAI]: Dark-store AGV ko warehouse floor par efficiently route karna.
    // [KAISE CHALTA HAI]: Open set PriorityQueue me lowest f-score explore karta hai, visited nodes closed set me track karta hai, aur goal reach hone par parent chain traverse karke path reconstruct karta hai.
    // [OUTPUT / SIDE EFFECT]: List of GridPoints representing shortest path, or empty if blocked.
    // [VARIATIONS TO TRY]: Jump Point Search (JPS) for 10x faster grid traversal.
    public List<GridPoint> FindPath(GridPoint start, GridPoint goal)
    {
        // [KYA KARTA HAI]: Start aur Goal validation check karta hai.
        // [KYU ZAROORI HAI]: Blocked ya out-of-bounds start/goal handle karna.
        // [KAISE CHALTA HAI]: IsWalkable checks.
        // [OUTPUT / SIDE EFFECT]: Empty list if invalid.
        // [VARIATIONS TO TRY]: Find nearest walkable neighbor if goal is on shelf.
        if (!IsWalkable(start.X, start.Y) || !IsWalkable(goal.X, goal.Y))
        {
            return new List<GridPoint>();
        }

        // [KYA KARTA HAI]: Open set PriorityQueue instantiate karta hai (element, fScore).
        // [KYU ZAROORI HAI]: $O(\log N)$ extraction of node with minimum $f = g + h$.
        // [KAISE CHALTA HAI]: PriorityQueue<GridPoint, int>.
        // [OUTPUT / SIDE EFFECT]: Open set queue.
        // [VARIATIONS TO TRY]: Custom Fibonacci heap.
        var openSet = new PriorityQueue<GridPoint, int>();

        // [KYA KARTA HAI]: G-Scores map store karta hai (point -> exact cost from start).
        // [KYU ZAROORI HAI]: Optimal path cost comparison.
        // [KAISE CHALTA HAI]: Dictionary<GridPoint, int>.
        // [OUTPUT / SIDE EFFECT]: G-score lookup.
        // [VARIATIONS TO TRY]: Flat array indexing for zero hashing overhead.
        var gScore = new Dictionary<GridPoint, int> { [start] = 0 };

        // [KYA KARTA HAI]: Came-From map store karta hai (child -> parent).
        // [KYU ZAROORI HAI]: Backtracking path reconstruction.
        // [KAISE CHALTA HAI]: Dictionary<GridPoint, GridPoint>.
        // [OUTPUT / SIDE EFFECT]: Route parent pointers.
        // [VARIATIONS TO TRY]: Direction vector storage.
        var cameFrom = new Dictionary<GridPoint, GridPoint>();

        // [KYA KARTA HAI]: Closed set HashSet store karta hai.
        // [KYU ZAROORI HAI]: Already explored nodes ko re-process hone se rokna.
        // [KAISE CHALTA HAI]: HashSet<GridPoint>.
        // [OUTPUT / SIDE EFFECT]: Closed nodes set.
        // [VARIATIONS TO TRY]: Visited bit array.
        var closedSet = new HashSet<GridPoint>();

        // [KYA KARTA HAI]: Start node open set me push karta hai.
        // [KYU ZAROORI HAI]: Begin search.
        // [KAISE CHALTA HAI]: openSet.Enqueue(start, h(start, goal)).
        // [OUTPUT / SIDE EFFECT]: Initial open node.
        // [VARIATIONS TO TRY]: Multiple simultaneous start points.
        openSet.Enqueue(start, ManhattanDistance(start, goal));

        // [KYA KARTA HAI]: 4-directional neighbor offsets (Up, Right, Down, Left).
        // [KYU ZAROORI HAI]: Orthogonal movement in warehouse aisles.
        // [KAISE CHALTA HAI]: (dx, dy) pairs.
        // [OUTPUT / SIDE EFFECT]: Neighbor offsets.
        // [VARIATIONS TO TRY]: 8 directions with diagonal cost 14 (~1.414).
        ReadOnlySpan<(int dx, int dy)> directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];

        // [KYA KARTA HAI]: A* search loop.
        // [KYU ZAROORI HAI]: Explore nodes until goal found or queue exhausted.
        // [KAISE CHALTA HAI]: while (openSet.Count > 0).
        // [OUTPUT / SIDE EFFECT]: Explores grid.
        // [VARIATIONS TO TRY]: Maximum step count limiter.
        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            // [KYA KARTA HAI]: Check karta hai kya current node Goal hai.
            // [KYU ZAROORI HAI]: Search completion detection.
            // [KAISE CHALTA HAI]: current.Equals(goal).
            // [OUTPUT / SIDE EFFECT]: Trigger path reconstruction.
            // [VARIATIONS TO TRY]: Proximity radius check.
            if (current == goal)
            {
                // [KYA KARTA HAI]: Goal se start tak parent pointers traverse karke path reconstruct karta hai.
                // [KYU ZAROORI HAI]: Final coordinates list return karna.
                // [KAISE CHALTA HAI]: While cameFrom contains node -> reverse list.
                // [OUTPUT / SIDE EFFECT]: Reconstructed path list.
                // [VARIATIONS TO TRY]: Smooth path using Bezier curves.
                var path = new List<GridPoint> { current };
                while (cameFrom.TryGetValue(current, out var parent))
                {
                    path.Add(parent);
                    current = parent;
                }
                path.Reverse();
                return path;
            }

            // [KYA KARTA HAI]: Current node ko closed set me mark karta hai.
            // [KYU ZAROORI HAI]: Do not re-evaluate.
            // [KAISE CHALTA HAI]: closedSet.Add(current).
            // [OUTPUT / SIDE EFFECT]: Node marked closed.
            // [VARIATIONS TO TRY]: Color node for visualization.
            closedSet.Add(current);

            int currentG = gScore[current];

            // [KYA KARTA HAI]: Sabhi 4 neighbors check karta hai.
            // [KYU ZAROORI HAI]: Valid movement exploration.
            // [KAISE CHALTA HAI]: foreach over directions.
            // [OUTPUT / SIDE EFFECT]: Evaluates adjacent cells.
            // [VARIATIONS TO TRY]: Turn penalty addition to avoid zigzagging.
            foreach (var (dx, dy) in directions)
            {
                int nx = current.X + dx;
                int ny = current.Y + dy;
                var neighbor = new GridPoint(nx, ny);

                // [KYA KARTA HAI]: Check karta hai kya neighbor walkable hai aur closed set me nahi hai.
                // [KYU ZAROORI HAI]: Avoid walls and loops.
                // [KAISE CHALTA HAI]: IsWalkable && !closedSet.Contains.
                // [OUTPUT / SIDE EFFECT]: Skip if invalid.
                // [VARIATIONS TO TRY]: Dynamic weight cost lookup.
                if (!IsWalkable(nx, ny) || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int tentativeG = currentG + 1; // Orthogonal step cost = 1

                // [KYA KARTA HAI]: Check karta hai kya ye path better hai than previously known path to neighbor.
                // [KYU ZAROORI HAI]: Relaxation step in shortest path algorithms.
                // [KAISE CHALTA HAI]: tentativeG < gScore[neighbor].
                // [OUTPUT / SIDE EFFECT]: Updates best route to neighbor.
                // [VARIATIONS TO TRY]: Strict inequality check.
                if (!gScore.TryGetValue(neighbor, out var existingG) || tentativeG < existingG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, goal);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        // [KYA KARTA HAI]: Path not found (fully blocked by obstacles).
        // [KYU ZAROORI HAI]: Graceful failure reporting.
        // [KAISE CHALTA HAI]: Return empty list.
        // [OUTPUT / SIDE EFFECT]: Empty list.
        // [VARIATIONS TO TRY]: Return closest partial path.
        return new List<GridPoint>();
    }

    // [KYA KARTA HAI]: Warehouse grid ko path ke sath ASCII format me render karta hai.
    // [KYU ZAROORI HAI]: Visual terminal inspection and educational debugging.
    // [KAISE CHALTA HAI]: Walkable = '.', Obstacle = '#', Path = '*', Start = 'S', Goal = 'G'.
    // [OUTPUT / SIDE EFFECT]: Multi-line ASCII string.
    // [VARIATIONS TO TRY]: ANSI color escape codes for terminal styling.
    public string RenderAscii(List<GridPoint> path, GridPoint start, GridPoint goal)
    {
        var pathSet = new HashSet<GridPoint>(path);
        var sb = new System.Text.StringBuilder();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var pt = new GridPoint(x, y);
                if (pt == start)
                {
                    sb.Append("S ");
                }
                else if (pt == goal)
                {
                    sb.Append("G ");
                }
                else if (pathSet.Contains(pt))
                {
                    sb.Append("* ");
                }
                else if (!_grid[x, y])
                {
                    sb.Append("# ");
                }
                else
                {
                    sb.Append(". ");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

// [KYA KARTA HAI]: 2D grid coordinates point encapsulate karne wala readonly struct.
// [KYU ZAROORI HAI]: Zero heap allocation point representation.
// [KAISE CHALTA HAI]: Struct value equality.
// [OUTPUT / SIDE EFFECT]: Lightweight 2D coordinate.
// [VARIATIONS TO TRY]: 3D coordinate struct (X, Y, Z).
public readonly record struct GridPoint(int X, int Y)
{
    public override string ToString() => $"({X},{Y})";
}
