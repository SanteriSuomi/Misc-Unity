using Spellplague.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Spellplague.AI
{
    public class PathfinderDebugger : MonoBehaviour
    {
        [SerializeField]
        private Transform pathfinderDebuggerEnd = default;
        private List<Vector3> path;
        private Pathfinder pathfinder;
        private PriorityQueue<Node> priorityQueue;

        private void Awake()
        {
            path = new List<Vector3>();
            pathfinder = new Pathfinder(transform);
            priorityQueue = new PriorityQueue<Node>();
        }

        private void Update()
        {
            Input();
        }

        private void Input()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                pathfinder.Move(transform, transform.position, pathfinderDebuggerEnd.position, 2.5f, 5);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                pathfinder.Stop();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                for (int i = 0; i < 1000; i++)
                {
                    var node = new Node(Vector3.zero, Vector2.zero, false, 1)
                    {
                        G = Random.Range(0, 100),
                        H = Random.Range(0, 100)
                    };

                    priorityQueue.Push(node);
                }

                foreach (var item in priorityQueue)
                {
                    Debug.Log(item.F);
                }
            }
        }

        private void OnDisable() => pathfinder.Stop();

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                pathfinder.Stop();
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (path == null || path.Count <= 0) { return; }
            Gizmos.color = Color.white;
            int pathCount = path.Count;
            for (int i = 0; i < pathCount; i++)
            {
                Gizmos.DrawWireSphere(path[i], 0.2f);
            }
        }
        #endif
    }
}