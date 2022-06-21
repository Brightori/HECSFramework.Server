using HECSFramework.Core;
using Helpers;
using System.Numerics;

namespace Components
{
    public partial class TransformComponent : IAfterSerializationComponent
    {
        private Vector3 position;

        public Vector3 GetPosition
        {
            get => position;
            set
            {
                position = value;
                PositionSave = new Vector3Serialize(position);
            }
        }

        public void SetPosition(Vector3 position)
        {
            GetPosition = position;
            InfoUpdated();
        }

        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;
            InfoUpdated();
        }

        public Quaternion Rotation
        {
            get => RotationSave.AsNumericsVector().Quaternion();
            set => RotationSave = new Vector3Serialize(value.ToEuler());
        }

        public void Translate(Vector3 direction)
        {
            GetPosition += direction;
        }

        private void AfterSyncCompleted()
        {
            position = PositionSave.AsNumericsVector();
        }
        
        public void AfterSync()
        {
            InfoUpdated();
            AfterSyncCompleted();
        }
    }
}