using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spellplague.AI
{
    public class Pathfinder
    {
        public Transform PathfinderTransform { get; set; }

        /// <summary>
        /// Minimum distance used in calculating first node and such.
        /// </summary>
        public float MinimumDistance { get; set; } = 1.1f;

        /// <summary>
        /// Distance from the target at which to stop at.
        /// </summary>
        public float StoppingDistance { get; set; } = 0.8f;

        /// <summary>
        /// At what radius should the object be treated as, for path smoothing.
        /// </summary>
        public float ObjectRadius { get; set; } = 0.45f;

        /// <summary>
        /// Up how much from the ground should the path smoother start from. 
        /// Should probably be higher than radius.
        /// </summary>
        public float SmoothRayOffset { get; set; } = 0.475f;

        /// <summary>
        /// Up how much from the ground should the character's center be when moving.
        /// </summary>
        public float MovePositionOffset { get; set; } = 0.5f;

        private const float MinFloat = 0.0000000596F;
        private const float UpdateMultiplier = 1000;

        private readonly List<NodeGrid> grids;
        /// <summary>
        /// Transform that will be used to find the nearest grid.
        /// </summary>
        /// <param name="pathfinderTransform"></param>
        public Pathfinder(Transform pathfinderTransform)
        {
            PathfinderTransform = pathfinderTransform;
            grids = UnityEngine.Object.FindObjectsOfType<NodeGrid>().ToList();
            if (grids.Count <= 0)
            {
                Debug.LogWarning("Pathfinder requires at least 1 grid for pathfinding.");
            }
        }

        private bool canMove;
        /// <summary>
        /// Explicitly stop ongoing moving.
        /// </summary>
        public void Stop() => canMove = false;

        /// <summary>
        /// Find a path for a character and start moving along the path.
        /// </summary>
        /// <param name="objectToMove"></param>
        /// <param name="start"></param>
        /// <param name="destination"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="turnSpeed"></param>
        /// <param name="objectRadius"></param>
        public async void Move(Transform objectToMove, Vector3 start, Vector3 destination,
            float moveSpeed, float turnSpeed)
        {
            Stop();
            NodeGrid grid = GetClosestGrid();
            while (grid.IsBeingUsed && objectToMove != null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Time.deltaTime * UpdateMultiplier));
            }

            grid.IsBeingUsed = true;
            List<Vector3> path = await Task.Run(() => GetPath(start, destination, grid));
            grid.IsBeingUsed = false;

            if (path.Count > 0)
            {
                canMove = true;
                path = await SmoothPath(path);
                await MoveAlongPath(objectToMove, path, moveSpeed, turnSpeed);
            }
        }

        private NodeGrid GetClosestGrid()
        {
            NodeGrid closestGrid = grids[0];
            if (grids.Count == 1)
            {
                return closestGrid;
            }

            float smallestDistance = Mathf.Infinity;
            int gridsCount = grids.Count - 1;
            for (int i = 0; i < gridsCount; i++)
            {
                float gridSqrDistance = GetSqrDistance(grids[i].transform.position,
                    PathfinderTransform.position);
                if (gridSqrDistance <= MinimumDistance)
                {
                    return grids[i];
                }
                else if (gridSqrDistance < smallestDistance)
                {
                    closestGrid = grids[i];
                }
            }

            return closestGrid;
        }

        private async Task<List<Vector3>> SmoothPath(List<Vector3> inputPath)
        {
            if (inputPath.Count <= 2) { return inputPath; }
            List<Vector3> outputPath = new List<Vector3>
            {
                inputPath[0]
            };

            int inputPathCount = inputPath.Count - 1;
            for (int i = 2; i < inputPathCount; i++)
            {
                Vector3 castDirection = inputPath[i] - outputPath[outputPath.Count - 1];
                Ray castRay = new Ray(outputPath[outputPath.Count - 1]
                    + new Vector3(0, SmoothRayOffset, 0), castDirection.normalized);
                if (Physics.SphereCast(castRay, ObjectRadius, castDirection.magnitude))
                {
                    outputPath.Add(inputPath[i - 1]);
                }

                await Task.Delay(0);
            }

            outputPath.Add(inputPath[inputPath.Count - 1]);
            return outputPath;
        }

        private async Task MoveAlongPath(Transform transformToMove, List<Vector3> path, float moveSpeed,
            float turnSpeed)
        {
            int pathLastIndex = path.Count - 1;
            int pathSecondLastIndex = path.Count - 2;

            int pathIndex = 0;
            while (canMove && CheckPosition(transformToMove.position, path[pathLastIndex]))
            {
                path[pathIndex] += new Vector3(0, MovePositionOffset, 0);
                Vector3 currentDirection = (path[pathIndex] - transformToMove.position).normalized;
                currentDirection.y = 0;
                while (canMove && CheckPosition(transformToMove.position, path[pathIndex]))
                {
                    if (pathIndex == pathLastIndex || pathIndex == pathSecondLastIndex)
                    {
                        Vector3 rayDirection = path[pathLastIndex] - transformToMove.position;
                        if (Physics.Raycast(transformToMove.position, rayDirection.normalized, StoppingDistance)
                            && rayDirection.sqrMagnitude <= MinimumDistance)
                        {
                            Stop();
                        }
                    }

                    transformToMove.position = Vector3.MoveTowards(transformToMove.position, path[pathIndex],
                        moveSpeed * Time.deltaTime);
                    transformToMove.rotation = Quaternion.Slerp(transformToMove.rotation,
                        Quaternion.LookRotation(currentDirection, transformToMove.up), turnSpeed * Time.deltaTime);

                    await Task.Delay(TimeSpan.FromMilliseconds(Time.deltaTime * UpdateMultiplier));
                }

                pathIndex++;
            }

            bool CheckPosition(Vector3 currentPosition, Vector3 goalPosition)
            {

                return !(ApproximatelyEqual(currentPosition.x, goalPosition.x)
                      && ApproximatelyEqual(currentPosition.y, goalPosition.y)
                      && ApproximatelyEqual(currentPosition.z, goalPosition.z));
            }

            bool ApproximatelyEqual(float a, float b)
            {
                // Faster float number equal approximation
                return a + MinFloat >= b && a - MinFloat <= b;
            }
        }

        private async Task<List<Vector3>> GetPath(Vector3 start, Vector3 goal, NodeGrid grid)
        {
            PriorityQueue<Node> openQueue = new PriorityQueue<Node>();
            List<Node> modifiedNodes = new List<Node>();
            Node[,] nodeArray = grid.GetNodeArray();
            Node[,] nodeArrayOther = grid.GetNodeArrayOther();
            openQueue.Push(GetStartingNode(nodeArray, nodeArrayOther, start).Result);

            while (openQueue.Count > 0)
            {
                Node currentNode = openQueue.Pop();
                if (GetSqrDistance(goal, currentNode.Position) <= MinimumDistance)
                {
                    List<Vector3> path = new List<Vector3>();
                    Node pathNode = currentNode;
                    while (!(pathNode is null))
                    {
                        path.Add(pathNode.Position);
                        pathNode = pathNode.ParentNode;
                    }

                    await ResetPathfinder(modifiedNodes);

                    path.Reverse();
                    path.Add(goal);

                    return path;
                }

                currentNode.NodeState = NodeState.Closed;

                int nodeColumn = (int)currentNode.ArrayPosition.x;
                int nodeRow = (int)currentNode.ArrayPosition.y;

                // North
                Task north = CalculateChild(GetChild(nodeColumn, nodeRow - 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // North-East
                Task northEast = CalculateChild(GetChild(nodeColumn + 1, nodeRow - 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // East
                Task east = CalculateChild(GetChild(nodeColumn + 1, nodeRow,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // South-East
                Task southEast = CalculateChild(GetChild(nodeColumn + 1, nodeRow + 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // South
                Task south = CalculateChild(GetChild(nodeColumn, nodeRow + 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // South-West
                Task southWest = CalculateChild(GetChild(nodeColumn - 1, nodeRow + 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // West
                Task west = CalculateChild(GetChild(nodeColumn - 1, nodeRow,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);
                // North-West
                Task northWest = CalculateChild(GetChild(nodeColumn - 1, nodeRow - 1,
                        nodeArray, nodeArrayOther), currentNode, openQueue, modifiedNodes, goal);

                await Task.WhenAll(north, northEast, east, southEast, south, southWest, west, northWest);
            }

            await ResetPathfinder(modifiedNodes);

            #if UNITY_EDITOR
            Debug.Log("No path found. Check that grids are available for pathfinding.");
            #endif

            return new List<Vector3>();
        }

        private async Task<Node> GetStartingNode(Node[,] nodeArray, Node[,] nodeArrayOther, Vector3 startingPosition)
        {
            Task<Node> getNodePositive = GetNodePositive(nodeArray, nodeArrayOther, startingPosition);
            Task<Node> getNodeNegative = GetNodeNegative(nodeArray, nodeArrayOther, startingPosition);
            return await Task.WhenAny(getNodePositive, getNodeNegative).Result;
        }

        private async Task<Node> GetNodePositive(Node[,] nodeArray, Node[,] nodeArrayOther,
            Vector3 startingPosition)
        {
            Node currentNode = null;
            float currentLowest = Mathf.Infinity;
            bool earlyExit = false;
            for (int column = 0; column < nodeArray.GetLength(0); column++)
            {
                for (int row = 0; row < nodeArray.GetLength(1); row++)
                {
                    Node arrayNode = nodeArray[column, row];
                    if (arrayNode.IsWalkable)
                    {
                        float arrayNodeDistance = GetSqrDistance(arrayNode.Position, startingPosition);
                        if (arrayNodeDistance <= MinimumDistance)
                        {
                            currentNode = arrayNode;
                            earlyExit = true;
                            break;
                        }
                        else if (arrayNodeDistance < currentLowest)
                        {
                            currentNode = arrayNode;
                        }
                    }

                    Node arrayNodeOther = nodeArrayOther[column, row];
                    if (arrayNodeOther.IsWalkable)
                    {
                        float arrayNodeOtherDistance = GetSqrDistance(arrayNodeOther.Position, startingPosition);
                        if (arrayNodeOtherDistance <= MinimumDistance)
                        {
                            currentNode = arrayNodeOther;
                            earlyExit = true;
                            break;
                        }
                        else if (arrayNodeOtherDistance < currentLowest)
                        {
                            currentNode = arrayNodeOther;
                        }
                    }

                    await Task.Delay(0);
                }

                if (earlyExit)
                {
                    break;
                }
            }
            return currentNode;
        }

        private async Task<Node> GetNodeNegative(Node[,] nodeArray, Node[,] nodeArrayOther,
            Vector3 startingPosition)
        {
            Node currentNode = null;
            float currentLowest = Mathf.Infinity;
            bool earlyExit = false;
            for (int column = nodeArray.GetLength(0); column > 0; column--)
            {
                for (int row = nodeArray.GetLength(1); row > 0; row--)
                {
                    Node arrayNode = nodeArray[column, row];
                    if (arrayNode.IsWalkable)
                    {
                        float arrayNodeDistance = GetSqrDistance(arrayNode.Position, startingPosition);
                        if (arrayNodeDistance <= MinimumDistance)
                        {
                            currentNode = arrayNode;
                            earlyExit = true;
                            break;
                        }
                        else if (arrayNodeDistance < currentLowest)
                        {
                            currentNode = arrayNode;
                        }
                    }

                    Node arrayNodeOther = nodeArrayOther[column, row];
                    if (arrayNodeOther.IsWalkable)
                    {
                        float arrayNodeOtherDistance = GetSqrDistance(arrayNodeOther.Position, startingPosition);
                        if (arrayNodeOtherDistance <= MinimumDistance)
                        {
                            currentNode = arrayNodeOther;
                            earlyExit = true;
                            break;
                        }
                        else if (arrayNodeOtherDistance < currentLowest)
                        {
                            currentNode = arrayNodeOther;
                        }
                    }

                    await Task.Delay(0);
                }

                if (earlyExit)
                {
                    break;
                }
            }

            return currentNode;
        }

        private static async Task<ValueTuple<Node, Node>> GetChild(int column, int row, Node[,] nodeArray,
            Node[,] nodeArrayOther)
        {
            if (column > nodeArray.GetLowerBound(0)
                && column < nodeArray.GetUpperBound(0)
                && row > nodeArray.GetLowerBound(1)
                && row < nodeArray.GetUpperBound(1))
            {
                Node arrayNode = nodeArray[column, row];
                if (!arrayNode.IsWalkable)
                {
                    arrayNode = null;
                }

                Node arrayNodeOther = nodeArrayOther[column, row];
                if (!arrayNodeOther.IsWalkable)
                {
                    arrayNodeOther = null;
                }

                await Task.Delay(0);

                return new ValueTuple<Node, Node>(arrayNode, arrayNodeOther);
            }

            return new ValueTuple<Node, Node>(null, null);
        }

        private async Task CalculateChild(Task<ValueTuple<Node, Node>> nodes, Node currentNode,
            PriorityQueue<Node> openQueue, List<Node> modifiedNodes, Vector3 goal)
        {
            CalculateHeuristics(nodes.Result.Item1);
            CalculateHeuristics(nodes.Result.Item2);

            await Task.Delay(0);

            void CalculateHeuristics(Node node)
            {
                if (!(node is null)
                    && node.NodeState != NodeState.Closed)
                {
                    modifiedNodes.Add(node);

                    // Heuristics
                    float distanceToChild = GetSqrDistance(node.Position, currentNode.Position)
                        * node.DifficultyMultiplier;
                    node.G = currentNode.G + distanceToChild;
                    float distanceToGoal = GetSqrDistance(goal, node.Position);
                    node.H = distanceToGoal;
                    node.ParentNode = currentNode;

                    if (node.NodeState != NodeState.Open)
                    {
                        node.NodeState = NodeState.Open;
                        openQueue.Push(node);
                    }
                }
            }
        }

        private static float GetSqrDistance(Vector3 goal, Vector3 start)
        {
            Vector3 vector = goal - start;
            return vector.x * vector.x
                 + vector.y * vector.y
                 + vector.z * vector.z;
        }

        private async static Task ResetPathfinder(List<Node> modifiedNodes)
        {
            int modifiedNodesCount = modifiedNodes.Count - 1;
            for (int i = 0; i < modifiedNodesCount; i++)
            {
                modifiedNodes[i].Reset();
                await Task.Delay(0);
            }
        }
    }
}