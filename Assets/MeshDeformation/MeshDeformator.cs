using System.Collections;
using System.Collections.Generic;

public class MeshDeformator : MonoBehaviour
{
    private MeshFilter objMeshFilter;
    private List<Vector3> objectVertices;
    private List<Vector3> hitVertices;

    [SerializeField]
    private float deformationAreaSize = 1.5f;
    [SerializeField]
    private float deformationDepth = 0.02f;

    private void Awake()
    {
        objectVertices = new List<Vector3>();
        hitVertices = new List<Vector3>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.TryGetComponent(out objMeshFilter))
        {
            objMeshFilter.mesh.GetVertices(objectVertices);
            #if UNITY_EDITOR
            Debug.Log(objectVertices.Count);
            #endif
            if (objectVertices.Count > 0)
            {
                StartCoroutine(GetVerticesNearImpact(collision));
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log("Vertex list empty.");
                #endif
            }
        }
    }

    private IEnumerator GetVerticesNearImpact(Collision collision)
    {
        GetCollisionInfo(collision, out Vector3 contactPoint, out Vector3 hitDirection);
        for (int i = 0; i < objectVertices.Count; i++)
        {
            if ((collision.transform.TransformPoint(objectVertices[i]) - contactPoint).sqrMagnitude <= deformationAreaSize)
            {
                hitVertices.Add(objectVertices[i] + (-hitDirection * deformationDepth));
            }
            else
            {
                hitVertices.Add(objectVertices[i]);
            }

            yield return null;
        }

        UpdateMesh();
    }

    private static void GetCollisionInfo(Collision collision, 
        out Vector3 contactPoint, 
        out Vector3 hitDirection)
    {
        ContactPoint collisionContact = collision.contacts[0];
        contactPoint = collisionContact.point;
        hitDirection = collisionContact.normal;
    }

    private void UpdateMesh()
    {
        objMeshFilter.mesh.SetVertices(hitVertices);
        objMeshFilter.mesh.RecalculateNormals();
    }
}