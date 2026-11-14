namespace KiranaHub.DSA.Algorithms;

// =========================================================================================
// [KYA KARTA HAI]:
// Weighted Graph par Dijkstra's Shortest Path Algorithm implementation.
//
// [KYU ZAROORI HAI]:
// BFS sirf unweighted hops calculate karta hai (kitne turns/chowk hain).
// Lekin real life me do chaurahon ke beech traffic jam ya flyover ho sakta hai (time in minutes alag hota hai).
// Dijkstra's algorithm actual minimum travel time / distance path find karta hai.
//
// [KAISE CHALTA HAI]:
// Greedy strategy: Unvisited nodes me se minimum tentative distance node pick karta hai aur
// uske padosi rasto ko "Relax" karta hai: `newDist = currentDist + edgeWeight`.
//
// [OUTPUT EXPECTATION]:
// Warehouse se Customer address tak ka fastest route and total travel time in minutes return karega.
// =========================================================================================
public class DijkstraDeliveryRouter
{
    public record Edge(string Destination, double WeightMinutes);

    private readonly Dictionary<string, List<Edge>> _adjList = new();

    public void AddRoad(string u, string v, double weightMinutes)
    {
        if (!_adjList.ContainsKey(u)) _adjList[u] = new List<Edge>();
        if (!_adjList.ContainsKey(v)) _adjList[v] = new List<Edge>();

        _adjList[u].Add(new Edge(v, weightMinutes));
        _adjList[v].Add(new Edge(u, weightMinutes)); // Two-way road
    }

    public (List<string> Path, double TotalMinutes) FindFastestRoute(string start, string end)
    {
        if (!_adjList.ContainsKey(start) || !_adjList.ContainsKey(end))
            return (new List<string>(), -1);

        var distances = new Dictionary<string, double>();
        var previous = new Dictionary<string, string?>();
        var unvisited = new HashSet<string>(_adjList.Keys);

        foreach (var node in _adjList.Keys)
        {
            distances[node] = double.PositiveInfinity;
            previous[node] = null;
        }
        distances[start] = 0;

        while (unvisited.Count > 0)
        {
            // Pick unvisited node with smallest distance
            string current = unvisited.OrderBy(n => distances[n]).First();
            if (double.IsPositiveInfinity(distances[current]) || current == end)
            {
                break;
            }

            unvisited.Remove(current);

            foreach (var edge in _adjList[current])
            {
                if (unvisited.Contains(edge.Destination))
                {
                    double alt = distances[current] + edge.WeightMinutes;
                    if (alt < distances[edge.Destination])
                    {
                        distances[edge.Destination] = alt;
                        previous[edge.Destination] = current;
                    }
                }
            }
        }

        if (double.IsPositiveInfinity(distances[end]))
            return (new List<string>(), -1);

        // Reconstruct path
        var path = new List<string>();
        string? curr = end;
        while (curr != null)
        {
            path.Add(curr);
            curr = previous[curr];
        }
        path.Reverse();

        return (path, distances[end]);
    }
}

