using UnityEngine;

public class CameraTransformation : Transformation
{
    public enum CameraTranformationType { None, Orthographic, Perspective }

    public CameraTranformationType TranformationType = CameraTranformationType.None;
    public float focalLength = 1f;

    public override Matrix4x4 Matrix
    {
        get
        {
            Matrix4x4 matrix = new Matrix4x4();

            switch(TranformationType)
            {
                case CameraTranformationType.None:
                {
                    // identity matrix (diagonal ones) does not change the result
                    matrix.SetRow(0, new Vector4(1f, 0f, 0f, 0f));
                    matrix.SetRow(1, new Vector4(0f, 1f, 0f, 0f));
                    matrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
                    matrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
                    break;
                }
                case CameraTranformationType.Orthographic:
                {
                    // dropping Z dimension - flattering
                    matrix.SetRow(0, new Vector4(1f, 0f, 0f, 0f));
                    matrix.SetRow(1, new Vector4(0f, 1f, 0f, 0f));
                    matrix.SetRow(2, new Vector4(0f, 0f, 0f, 0f));
                    matrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));
                    break;
                }
                default: // Perspective
                {
                    matrix.SetRow(0, new Vector4(focalLength, 0f, 0f, 0f));
                    matrix.SetRow(1, new Vector4(0f, focalLength, 0f, 0f));
                    matrix.SetRow(2, new Vector4(0f, 0f, 0f, 0f));
                    matrix.SetRow(3, new Vector4(0f, 0f, 1f, 0f));
                    break;
                }
            }

            return matrix;

            /*
                If we were to fully mimic Unity's camera projection, we would also have to deal with the near and far plane. 
                That would require projecting into a cube instead of a plane, so depth information is retained. 
                Then there is the view aspect ratio to worry about. Also, Unity's camera looks in the negative Z direction, 
                which requires negating some numbers. You could incorporate all that into the projection matrix.
            */
        }
    }

    public override Vector3 Apply(Vector3 point)
    {
        throw new System.NotImplementedException();
    }
}
