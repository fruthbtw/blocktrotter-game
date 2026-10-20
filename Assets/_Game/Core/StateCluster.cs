using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>
    /// Every position reachable from a starting board, each labelled with its
    /// minimum number of moves to a solved position. Moves are reversible, so this
    /// is one connected component of the puzzle graph.
    /// </summary>
    /// <example>
    /// <code>
    /// StateCluster cluster = StateCluster.Explore(board, maxStates: 200_000);
    /// if (cluster != null &amp;&amp; cluster.MaxDistance &gt;= 20)
    ///     Board hard = cluster.GetBoardAtDistance(20, 0);
    /// </code>
    /// </example>
    public sealed class StateCluster
    {
        private readonly StateSpace _space;
        private readonly Dictionary<int, List<ulong>> _byDistance;

        /// <summary>Number of positions in the cluster.</summary>
        public int StateCount { get; }

        /// <summary>Largest minimum-move count of any position; -1 if nothing in the cluster is solvable.</summary>
        public int MaxDistance { get; }

        private StateCluster(StateSpace space, int stateCount, Dictionary<int, List<ulong>> byDistance, int maxDistance)
        {
            _space = space;
            StateCount = stateCount;
            _byDistance = byDistance;
            MaxDistance = maxDistance;
        }

        /// <summary>
        /// Explores the component containing <paramref name="board"/>. Returns null
        /// when it exceeds <paramref name="maxStates"/>, because distances in a
        /// partially explored component are not reliable.
        /// </summary>
        public static StateCluster Explore(Board board, int maxStates)
        {
            var space = new StateSpace(board);
            var neighbors = new List<(ulong Key, Move Move)>();

            // 1. Collect the component.
            var states = new List<ulong>();
            var seen = new HashSet<ulong>();
            ulong start = space.StartKey;
            seen.Add(start);
            states.Add(start);
            for (int i = 0; i < states.Count; i++)
            {
                space.GetNeighbors(states[i], neighbors);
                foreach ((ulong next, _) in neighbors)
                {
                    if (!seen.Add(next)) continue;
                    if (seen.Count > maxStates) return null;
                    states.Add(next);
                }
            }

            // 2. Multi-source BFS from every solved position.
            var distance = new Dictionary<ulong, int>(states.Count);
            var queue = new Queue<ulong>();
            foreach (ulong s in states)
            {
                if (!space.IsSolved(s)) continue;
                distance[s] = 0;
                queue.Enqueue(s);
            }
            int maxDistance = queue.Count > 0 ? 0 : -1;
            var byDistance = new Dictionary<int, List<ulong>>();
            while (queue.Count > 0)
            {
                ulong key = queue.Dequeue();
                int d = distance[key];
                space.GetNeighbors(key, neighbors);
                foreach ((ulong next, _) in neighbors)
                {
                    if (distance.ContainsKey(next)) continue;
                    distance[next] = d + 1;
                    if (d + 1 > maxDistance) maxDistance = d + 1;
                    if (!byDistance.TryGetValue(d + 1, out var list))
                        byDistance[d + 1] = list = new List<ulong>();
                    list.Add(next);
                    queue.Enqueue(next);
                }
            }
            return new StateCluster(space, states.Count, byDistance, maxDistance);
        }

        /// <summary>Number of positions exactly <paramref name="distance"/> moves from solved (distance ≥ 1).</summary>
        public int CountAtDistance(int distance) =>
            _byDistance.TryGetValue(distance, out var list) ? list.Count : 0;

        /// <summary>The <paramref name="index"/>-th position at the given distance (distance ≥ 1).</summary>
        public Board GetBoardAtDistance(int distance, int index) => _space.ToBoard(_byDistance[distance][index]);
    }
}
