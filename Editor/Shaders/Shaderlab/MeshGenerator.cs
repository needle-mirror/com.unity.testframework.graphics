namespace UnityEngine.TestTools.Graphics.Shaders
{
    class MeshGenerator : MonoBehaviour
    {
        // https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html
        public static GameObject GenerateQuad(Shader shader, float width = 1, float height = 1)
        {
            var gameObject = new GameObject("Quad");

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(shader);

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            var mesh = new Mesh();

            Vector3[] vertices = { new(0, 0, 0), new(width, 0, 0), new(0, height, 0), new(width, height, 0) };

            mesh.vertices = vertices;

            int[] tris =
            {
                // lower left triangle
                0,
                2,
                1,
                // upper right triangle
                2,
                3,
                1,
            };
            mesh.triangles = tris;

            Vector3[] normals = { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };

            mesh.normals = normals;

            Vector2[] uv = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };

            mesh.uv = uv;

            meshFilter.mesh = mesh;

            // Put quad in the world center
            gameObject.transform.position = new Vector3(
                gameObject.transform.position.x - width / 2,
                gameObject.transform.position.y - height / 2,
                gameObject.transform.position.z
            );

            return gameObject;
        }
    }
}
