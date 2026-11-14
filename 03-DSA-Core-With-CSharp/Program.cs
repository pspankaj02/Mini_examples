using System;
using System.Diagnostics;
using KiranaHub.DSA.Algorithms;
using KiranaHub.DSA.DataStructures;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("==========================================================================");
Console.WriteLine("   KIRANAHUB - 03: DATA STRUCTURES & ALGORITHMS (DSA) IN C# (ENRICHED)   ");
Console.WriteLine("==========================================================================\n");

// ----------------------------------------------------------------------------------------
// 1. CUSTOM LINKED LIST
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [1] Custom LinkedList (Order Stream)");
var orderList = new CustomLinkedList<string>();
orderList.AddLast("ORD-001 (Milk)");
orderList.AddLast("ORD-002 (Bread)");
orderList.AddFirst("ORD-000 (Urgent Medicine)"); // Inserted at head in O(1)

string removedHead = orderList.RemoveFirst();
Console.WriteLine($"Removed urgent head item: {removedHead}");
Debug.Assert(removedHead.Contains("ORD-000"), "LinkedList RemoveFirst failed!");
Console.WriteLine($"Remaining items count: {orderList.Count}");
Console.WriteLine("[PASS] Custom LinkedList verified.\n");

// ----------------------------------------------------------------------------------------
// 2. CUSTOM STACK (CART UNDO)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [2] Custom Stack (Cart Action Undo - LIFO)");
var cartHistory = new CustomStack<string>();
cartHistory.Push("Added Amul Butter");
cartHistory.Push("Added Britannia Toast");
cartHistory.Push("Added Maggi 4-Pack");

Console.WriteLine($"User clicked Undo -> Reverted: {cartHistory.Pop()}");
Console.WriteLine($"Top action now: {cartHistory.Peek()}");
Debug.Assert(cartHistory.Peek() == "Added Britannia Toast", "Stack LIFO failed!");
Console.WriteLine("[PASS] Custom Stack verified.\n");

// ----------------------------------------------------------------------------------------
// 3. PRIORITY QUEUE (VIP EXPRESS ORDERS)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [3] Min-Heap Priority Queue (Express vs Standard Orders)");
var priorityQueue = new CustomPriorityQueue<string, int>();
priorityQueue.Enqueue("Order #101: Standard Rice Bag", priority: 3);
priorityQueue.Enqueue("Order #102: VIP Gold 10-Min Rush Ice Cream", priority: 1);
priorityQueue.Enqueue("Order #103: Standard Biscuits", priority: 3);
priorityQueue.Enqueue("Order #104: Silver Express Delivery", priority: 2);

string firstDispatched = priorityQueue.Dequeue();
Console.WriteLine($"Dispatched First: {firstDispatched}");
Debug.Assert(firstDispatched.Contains("VIP Gold"), "Priority Queue didn't pick lowest priority rank 1!");
Console.WriteLine("[PASS] Priority Queue Min-Heap verified.\n");

// ----------------------------------------------------------------------------------------
// 4. BINARY SEARCH TREE (BST)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [4] Binary Search Tree (Product Price Index)");
var priceTree = new BinarySearchTree<decimal, string>();
priceTree.Insert(150m, "Basmati Rice 1kg");
priceTree.Insert(33m, "Amul Milk 500ml");
priceTree.Insert(250m, "Olive Oil 500ml");
priceTree.Insert(12m, "Matchbox Pack");

bool found = priceTree.TryFind(33m, out string? item);
Console.WriteLine($"Search for Rs. 33 -> Found: {item}");
Debug.Assert(found && item == "Amul Milk 500ml", "BST search failed!");

var sortedItems = priceTree.InOrderTraversal().ToList();
Console.WriteLine($"Sorted in-order catalog by price: {string.Join(" -> ", sortedItems)}");
Debug.Assert(sortedItems[0] == "Matchbox Pack" && sortedItems[^1] == "Olive Oil 500ml");
Console.WriteLine("[PASS] BST search & sorted in-order traversal verified.\n");

// ----------------------------------------------------------------------------------------
// 5. CUSTOM HASH TABLE WITH COLLISION CHAINING
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [5] Custom Hash Table (SKU/Barcode O(1) Lookup)");
var skuTable = new CustomHashTable<string, decimal>(capacity: 4); // Small capacity to force hash collision
skuTable.Put("890103001", 33.0m);
skuTable.Put("890103002", 245.0m);
skuTable.Put("890103003", 120.0m);
skuTable.Put("890103004", 45.0m);

bool gotSku = skuTable.TryGet("890103002", out decimal skuPrice);
Console.WriteLine($"Lookup Barcode '890103002' -> Price: Rs. {skuPrice}");
Debug.Assert(gotSku && skuPrice == 245.0m, "HashTable lookup failed!");
Console.WriteLine("[PASS] Custom Hash Table with collision handling verified.\n");

// ----------------------------------------------------------------------------------------
// 6. SORTING ALGORITHMS (MERGE SORT & QUICK SORT)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [6] Sorting Algorithms (MergeSort & QuickSort)");
decimal[] prices1 = { 245m, 33m, 120m, 12m, 500m, 65m };
SortingAlgorithms.MergeSort(prices1);
Console.WriteLine($"MergeSorted Prices: {string.Join(", ", prices1)}");
Debug.Assert(prices1[0] == 12m && prices1[^1] == 500m, "MergeSort failed!");

decimal[] prices2 = { 99m, 15m, 350m, 45m, 22m };
SortingAlgorithms.QuickSort(prices2);
Console.WriteLine($"QuickSorted Prices: {string.Join(", ", prices2)}");
Debug.Assert(prices2[0] == 15m && prices2[^1] == 350m, "QuickSort failed!");
Console.WriteLine("[PASS] Sorting algorithms verified.\n");

// ----------------------------------------------------------------------------------------
// 7. GRAPH BFS (DELIVERY ROUTE FINDER - UNWEIGHTED HOPS)
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [7] Delivery Route Graph (BFS Shortest Hops)");
var routeGraph = new DeliveryRouteGraph();
routeGraph.AddRoad("Central Warehouse", "Sector 1 Hub");
routeGraph.AddRoad("Central Warehouse", "Sector 4 Hub");
routeGraph.AddRoad("Sector 1 Hub", "Greenwood Society");
routeGraph.AddRoad("Sector 4 Hub", "Sector 5 Cross");
routeGraph.AddRoad("Sector 5 Cross", "Greenwood Society");

var shortestRoute = routeGraph.FindShortestHopPath("Central Warehouse", "Greenwood Society");
Console.WriteLine($"Shortest Hop Delivery Route: {string.Join(" -> ", shortestRoute)}");
Debug.Assert(shortestRoute.Count == 3, "BFS shortest path should have 3 nodes: Central -> Sec 1 -> Greenwood!");
Console.WriteLine("[PASS] Graph BFS verified.\n");

// ----------------------------------------------------------------------------------------
// 8. TRIE (PREFIX TREE - INSTANT AUTOCOMPLETE) [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [8] Trie (Prefix Tree - Instant Search Autocomplete)");
var trie = new Trie();
trie.Insert("Amul Butter");
trie.Insert("Amul Milk");
trie.Insert("Amul Cheese");
trie.Insert("Amul Curd");
trie.Insert("Aashirvaad Atta");
trie.Insert("Britannia Bread");

var suggestions = trie.GetAutocompleteSuggestions("amu");
Console.WriteLine($"Search autocomplete for 'amu': {string.Join(", ", suggestions)}");
Debug.Assert(suggestions.Count == 4 && suggestions.Contains("Amul Butter") && suggestions.Contains("Amul Milk"));
Debug.Assert(trie.Search("Amul Milk") == true);
Debug.Assert(trie.Search("Tata Salt") == false);
Console.WriteLine("[PASS] Trie prefix tree search and autocomplete verified.\n");

// ----------------------------------------------------------------------------------------
// 9. CIRCULAR BUFFER (ZERO-ALLOCATION GPS RING BUFFER) [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [9] Circular Ring Buffer (Rider Live GPS Stream)");
var gpsBuffer = new CircularBuffer<string>(capacity: 3);
gpsBuffer.Enqueue("Lat: 28.6139, Lon: 77.2090 (Connaught Place)");
gpsBuffer.Enqueue("Lat: 28.6200, Lon: 77.2150 (Mandi House)");
gpsBuffer.Enqueue("Lat: 28.6300, Lon: 77.2200 (ITO Bridge)");

// Buffer is full! Enqueueing 4th item overwrites the 1st item automatically in O(1)
gpsBuffer.Enqueue("Lat: 28.6400, Lon: 77.2300 (Laxmi Nagar)");

var currentPoints = gpsBuffer.ToList();
Console.WriteLine($"Latest 3 GPS breadcrumbs retained in ring buffer:");
foreach (var pt in currentPoints) Console.WriteLine($"   {pt}");
Debug.Assert(gpsBuffer.Count == 3);
Debug.Assert(currentPoints[0].Contains("Mandi House") && currentPoints[2].Contains("Laxmi Nagar"));
Console.WriteLine("[PASS] Circular ring buffer O(1) rollover verified.\n");

// ----------------------------------------------------------------------------------------
// 10. DIJKSTRA'S WEIGHTED SHORTEST PATH [NEW DEEP COMPONENT]
// ----------------------------------------------------------------------------------------
Console.WriteLine("--> [10] Dijkstra's Algorithm (Weighted Delivery Traffic Routing)");
var dijkstra = new DijkstraDeliveryRouter();
dijkstra.AddRoad("Hub", "Junction A", weightMinutes: 5.0);
dijkstra.AddRoad("Hub", "Junction B", weightMinutes: 12.0);
dijkstra.AddRoad("Junction A", "Junction C", weightMinutes: 3.0);
dijkstra.AddRoad("Junction C", "Customer Doorstep", weightMinutes: 4.0);
dijkstra.AddRoad("Junction B", "Customer Doorstep", weightMinutes: 2.0);

// Path 1: Hub -> Junction A -> Junction C -> Doorstep = 5 + 3 + 4 = 12 mins
// Path 2: Hub -> Junction B -> Doorstep = 12 + 2 = 14 mins
var (fastestPath, totalMinutes) = dijkstra.FindFastestRoute("Hub", "Customer Doorstep");
Console.WriteLine($"Fastest Delivery Route: {string.Join(" -> ", fastestPath)} (Total: {totalMinutes} mins)");
Debug.Assert(totalMinutes == 12.0 && fastestPath[1] == "Junction A");
Console.WriteLine("[PASS] Dijkstra weighted graph pathfinder verified.\n");

Console.WriteLine("==========================================================================");
Console.WriteLine("   [SUCCESS] ALL 10 DSA TOPICS & ALGORITHMS PASSED AND VERIFIED!          ");
Console.WriteLine("==========================================================================");
