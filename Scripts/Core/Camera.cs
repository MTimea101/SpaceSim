using Silk.NET.Maths;

namespace SpaceSim
{
    public class Camera
    {
        public Vector3D<float> Position { get; private set; } = new(0, 0, 5);
        public Vector3D<float> Front { get; private set; } = new(0, 0, -1);
        public Vector3D<float> Up { get; private set; } = new(0, 1, 0);
        public Vector3D<float> Right { get; private set; } = new(1, 0, 0);

        public void SetPosition(Vector3D<float> newPosition)
        {
            Position = newPosition;
        }

        public void SetFront(Vector3D<float> newFront)
        {
            Front = Vector3D.Normalize(newFront);
            UpdateVectors();
        }

        private void UpdateVectors()
        {
            // Right vektor 
            Right = Vector3D.Normalize(Vector3D.Cross(Front, new Vector3D<float>(0, 1, 0)));

            // Up vektor 
            Up = Vector3D.Normalize(Vector3D.Cross(Right, Front));
        }

        public Matrix4X4<float> GetViewMatrix()
        {
            return Matrix4X4.CreateLookAt(Position, Position + Front, Up);
        }
    }
}