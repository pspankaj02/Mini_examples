namespace KiranaHub.DSA.Algorithms;

// =========================================================================================
// [KYA KARTA HAI]:
// Undirected Graph data structure (Adjacency List) representing Kirana delivery hubs and localities.
//
// [KYU ZAROORI HAI]:
// Delivery partner ko hub se customer ki society tak reach karne ke liye fastest path nikalna hota hai.
// BFS (Breadth-First Search) unweighted graph me Shortest Path (minimum hops/junctions) guarantee karta hai.
// DFS (Depth-First Search) pure network connectivity check karne ke kaam aata hai.
//
// [KAISE CHALTA HAI]:
// - BFS: Queue use karta hai aur level-by-level explore karta hai.
// - DFS: Recursion ya Stack use karta hai aur ek raste ke depth tak explore karta hai.
// =========================================================================================
public class DeliveryRouteGraph
{
    private readonly Dictionary<string, List<string>> _adjList = new();

    public void AddLocation(string location)
    {
        if (!_adjList.ContainsKey(location))
        {
            _adjList[location] = new List<string>();
        }
    }

    public void AddRoad(string from, string to)
    {
        AddLocation(from);
        AddLocation(to);
        _adjList[from].Add(to);
        _adjList[to].Add(from); // Undirected road
    }

    // BREADTH-FIRST SEARCH (BFS): Finds shortest path in terms of hops
    public List<string> FindShortestHopPath(string start, string destination)
    {
        if (!_adjList.ContainsKey(start) || !_adjList.ContainsKey(destination))
            return new List<string>();

        var queue = new Queue<string>();
        var visited = new HashSet<string>();
        var parentMap = new Dictionary<string, string?>();

        queue.Enqueue(start);
        visited.Add(start);
        parentMap[start] = null;

        bool found = false;
        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (current == destination)
            {
                found = true;
                break;
            }

            foreach (var neighbor in _adjList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parentMap[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!found) return new List<string>();

        // Reconstruct path backwards from destination
        var path = new List<string>();
        string? step = destination;
        while (step != null)
        {
            path.Add(step);
            step = parentMap[step];
        }
        path.Reverse();
        return path;
    }

    // DEPTH-FIRST SEARCH (DFS): Traversal exploration
    public List<string> TraverseDfs(string start)
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        DfsHelper(start, visited, result);
        return result;
    }

    private void DfsHelper(string current, HashSet<string> visited, List<string> result)
    {
        if (!_adjList.ContainsKey(current) || visited.Contains(current)) return;

        visited.Add(current);
        result.Add(current);

        foreach (var neighbor in _adjList[current])
        {
            if (!visited.Contains(neighbor))
            {
                DfsHelper(neighbor, visited, result);
            }
        }
    }
}

