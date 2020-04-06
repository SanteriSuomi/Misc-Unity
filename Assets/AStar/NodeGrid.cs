using UnityEngine;

namespace Spellplague.AI
{
    public class NodeGrid : MonoBehaviour
    {
        private Node[,] nodeArray;
        public Node[,] GetNodeArray() 
            => nodeArray;

        private Node[,] nodeArrayOther;
        public Node[,] GetNodeArrayOther() 
            => nodeArrayOther;

        public bool IsBeingUsed { get; set; }

        [SerializeField]
        private LayerMask gridMakerLayerMask = default;

        [SerializeField]
        private int gridColumns = 50;
        [SerializeField]
        private int gridRows = 50;
        [SerializeField]
        private float gridColumnLength = 1;
        [SerializeField]
        private float gridRowLength = 1;
        [SerializeField]
        private float raycastHeight = 100;
        [SerializeField]
        private float raycastDistance = 200;
        [SerializeField]
        private float firstRayHitSphereRadius = 0.325f;
        [SerializeField]
        private float secondRayHitSphereRadius = 0.325f;
        [SerializeField]
        private float obstacleHitDifficultyMultiplier = 10;

        [SerializeField]
        [Tooltip("Tag for walkable outside areas.")]
        private string walkableTag = "Ground";
        [SerializeField]
        [Tooltip("Tag for walkable other (e.g inside) areas.")]
        private string walkableTagOther = "Inside";

        private void Awake()
        {
            nodeArray = new Node[gridColumns, gridRows];
            nodeArrayOther = new Node[gridColumns, gridRows];
            CreateGrid();
        }

        private void CreateGrid()
        {
            float gridSpacingAverage = (gridColumnLength + gridRowLength) / 2;
            Vector3 rayPosition = transform.position + new Vector3(CalculateOffset(gridColumns), raycastHeight, 
                CalculateOffset(gridRows));
            for (int column = 0; column < gridColumns; column++)
            {
                for (int row = 0; row < gridRows; row++)
                {
                    RaycastHit[] rayHits = Physics.RaycastAll(rayPosition, Vector3.down, raycastDistance, 
                        gridMakerLayerMask);
                    CheckRayForNode(column, row, rayHits, rayPosition - new Vector3(0, raycastHeight, 0));

                    if (row == gridRows - 1)
                    {
                        rayPosition.z = CalculateOffset(transform.position.z + gridRows);
                    }
                    else
                    {
                        rayPosition += new Vector3(0, 0, gridRowLength);
                    }
                }

                rayPosition += new Vector3(gridColumnLength, 0, 0);
            }

            float CalculateOffset(float baseValue)
            {
                // For centering grid to the gameobject.
                return baseValue / 2 * -1 * gridSpacingAverage;
            }
        }

        private void CheckRayForNode(int column, int row, RaycastHit[] rayHits, Vector3 elseRayPosition)
        {
            if (rayHits.Length == 1 && rayHits[0].collider.CompareTag(walkableTag))
            {
                Collider[] firstSphereTest = Physics.OverlapSphere(rayHits[0].point, firstRayHitSphereRadius, 
                    gridMakerLayerMask);
                if (firstSphereTest.Length <= 1)
                {
                    InitializeNewNode(column, row, rayHits[0].point, new Vector2(column, row), true, nodeArray, 
                        obstacleHitDifficultyMultiplier);
                }
                else
                {
                    InitializeNewNode(column, row, elseRayPosition, new Vector2(column, row), false, nodeArray, 1);
                }
            }
            else
            {
                InitializeNewNode(column, row, elseRayPosition, new Vector2(column, row), false, nodeArray, 1);
            }

            bool inside = false;
            int insideIndex = 0;
            for (int i = 0; i < rayHits.Length; i++)
            {
                if (rayHits[i].collider.CompareTag(walkableTagOther))
                {
                    inside = true;
                    insideIndex = i;
                }
            }

            if (rayHits.Length >= 2 && inside)
            {
                Collider[] secondSphereTest = Physics.OverlapSphere(rayHits[insideIndex].point, secondRayHitSphereRadius, 
                    gridMakerLayerMask);
                if (secondSphereTest.Length <= 2)
                {
                    InitializeNewNode(column, row, rayHits[insideIndex].point, new Vector2(column, row), true, nodeArrayOther, 
                        obstacleHitDifficultyMultiplier);
                }
                else
                {
                    InitializeNewNode(column, row, elseRayPosition, new Vector2(column, row), false, nodeArrayOther, 1);
                }
            }
            else
            {
                InitializeNewNode(column, row, elseRayPosition, new Vector2(column, row), false, nodeArrayOther, 1);
            }
        }

        private void InitializeNewNode(int column, int row, Vector3 position, Vector2 arrayPosition, bool isWalkable, 
            Node[,] nodeArray, float difficultyMultiplier)
        {
            Node newNode = new Node(position, arrayPosition, isWalkable, difficultyMultiplier);
            nodeArray[column, row] = newNode;
        }
        
        #region Debug Gizmos
        #if UNITY_EDITOR
        [SerializeField]
        private bool drawSpheresToggle = true;
        private enum DrawSphereMode
        {
            DrawNodeArray,
            DrawNodeArrayLower,
            DrawBoth
        }
        [SerializeField]
        private DrawSphereMode drawSphereMode = DrawSphereMode.DrawBoth;

        private void OnDrawGizmosSelected()
        {
            if (nodeArray == null || nodeArray.Length <= 0
                || nodeArrayOther == null || nodeArrayOther.Length <= 0
                || !drawSpheresToggle)
            {
                return;
            }

            LoopSpheres();
        }

        private void LoopSpheres()
        {
            for (int column = 0; column < nodeArray.GetLength(0); column++)
            {
                for (int row = 0; row < nodeArray.GetLength(1); row++)
                {
                    switch (drawSphereMode)
                    {
                        case DrawSphereMode.DrawNodeArray:
                            DrawSphere(column, row, nodeArray);
                            break;
                        case DrawSphereMode.DrawNodeArrayLower:
                            DrawSphere(column, row, nodeArrayOther);
                            break;
                        case DrawSphereMode.DrawBoth:
                            DrawSphere(column, row, nodeArray);
                            DrawSphere(column, row, nodeArrayOther);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void DrawSphere(int column, int row, Node[,] nodeArray)
        {
            Node nodeArrayNode = nodeArray[column, row];
            if (nodeArrayNode.IsWalkable)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawWireSphere(nodeArrayNode.Position, 0.075f);
        }
        #endif
        #endregion
    }
}