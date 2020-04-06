using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spellplague.Utility
{
    /// <summary>
    /// Combine objects to share a mesh. Don't forget to backup before usage!
    /// </summary>
    [ExecuteInEditMode]
    public class MeshCombiner : MonoBehaviour
    {
        [SerializeField]
        private Transform objectToCombine = default;
        [SerializeField]
        private Material materialToUse = default;

        private enum OldObjectSetting
        {
            None,
            Disable,
            Remove
        }

        [SerializeField]
        private OldObjectSetting whatToDoToOldObjects = default;

        private enum ColliderType
        {
            DontAdd,
            BoxCollider2D,
            CircleCollider2D,
            PolygonCollider2D,
            BoxCollider,
            SphereCollider,
            CapsuleCollider,
            MeshCollider
        }

        [SerializeField]
        private ColliderType colliderToAdd = ColliderType.DontAdd;

        [SerializeField]
        private bool includeParentMesh = false;
        [SerializeField]
        private bool mergeSubMeshes = true;
        [SerializeField]
        private bool includeTransformMatrices = true;
        [SerializeField]
        private bool includeLightMapData = true;

        public void Combine()
        {
            if (objectToCombine.childCount <= 0
                || !objectToCombine.TryGetComponent(out MeshFilter parentMeshFilter)
                || !objectToCombine.TryGetComponent(out MeshRenderer parentMeshRenderer))
            {
                Debug.LogError("Error, cannot combine, check that the parent has at least 1 child to combine, contains a mesh filter and a mesh renderer.");
                return;
            }

            List<MeshFilter> meshFilters = new List<MeshFilter>();
            if (includeParentMesh)
            {
                if (parentMeshFilter.mesh == null)
                {
                    Debug.LogError("You have selected include parent mesh, but parent mesh doesn't exist.");
                }
                else
                {
                    meshFilters.Add(parentMeshFilter);
                }
            }

            int amountOfChildrenWithoutMeshFilter = 0;
            for (int i = 0; i < objectToCombine.childCount; i++)
            {
                Transform child = objectToCombine.GetChild(i);
                if (child.TryGetComponent(out MeshFilter childMeshFilter))
                {
                    meshFilters.Add(childMeshFilter);
                }
                else
                {
                    amountOfChildrenWithoutMeshFilter++;
                }

                if (child.childCount >= 1)
                {
                    for (int j = 0; j < child.childCount; j++)
                    {
                        if (child.GetChild(j).TryGetComponent(out MeshFilter childChildMeshFilter))
                        {
                            meshFilters.Add(childChildMeshFilter);
                        }
                        else
                        {
                            amountOfChildrenWithoutMeshFilter++;
                        }
                    }
                }
            }

            if (amountOfChildrenWithoutMeshFilter > 0)
            {
                Debug.LogWarning($"Parent contained {amountOfChildrenWithoutMeshFilter} children without a mesh filter, is this intended?");
            }

            CombineInstance[] combineInstance = new CombineInstance[meshFilters.Count];

            for (int i = 0; i < meshFilters.Count; i++)
            {
                combineInstance[i].mesh = meshFilters[i].mesh;
                combineInstance[i].transform = meshFilters[i].transform.localToWorldMatrix;
                if (whatToDoToOldObjects == OldObjectSetting.Disable)
                {
                    meshFilters[i].gameObject.SetActive(false);
                }
                else if (whatToDoToOldObjects == OldObjectSetting.Remove)
                {
                    DestroyImmediate(meshFilters[i].gameObject);
                }
            }

            parentMeshFilter.mesh.CombineMeshes(combineInstance, mergeSubMeshes,
                includeTransformMatrices, includeLightMapData);
            parentMeshRenderer.material = materialToUse;
            if (colliderToAdd != ColliderType.DontAdd)
            {
                Type collider = Type.GetType(colliderToAdd.ToString());
                if (collider != null)
                {
                    objectToCombine.gameObject.AddComponent(collider);
                }
                else
                {
                    Debug.LogError($"Collider of the name {collider} does not exist. Make sure you typed it correctly (e.g BoxCollider, MeshCollider)");
                }
            }

            objectToCombine.gameObject.SetActive(true);
        }
    }
}