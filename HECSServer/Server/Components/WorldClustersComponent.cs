using HECSFramework.Core;
using MessagePack;
using System;
using System.Collections.Generic;

namespace Components
{
    public class WorldClustersComponent : BaseComponent, ILateStart
    {
        public static readonly Vector2Serialize ClusterStep = new Vector2Serialize { X = 2.5f, Y = 2.5f };

        public readonly int GridSizeY = 50;
        public readonly int GridSizeX = 50;

        public Dictionary<Vector2Serialize, List<IEntity>> Clusters = new Dictionary<Vector2Serialize, List<IEntity>>(128);

        public void LateStart()
        {
            var xStep = ClusterStep.X;
            var yStep = ClusterStep.Y;

            for (int i = 0; i < GridSizeX; i++)
            {
                var y = 0;
                var clusterRight = new Bound2d(new Vector2Serialize { X = xStep * i * 2, Y = y }, ClusterStep);
                var clusterLeft = new Bound2d(new Vector2Serialize { X = xStep * -i * 2, Y = y }, ClusterStep);

                AddCluster(clusterRight);
                AddCluster(clusterLeft);

                for (int j = 1; j < GridSizeY; j++)
                {
                    var upRight = new Bound2d(new Vector2Serialize { X = xStep * i * 2, Y = (yStep * 2) * j*2 }, ClusterStep);
                    var upLeft = new Bound2d(new Vector2Serialize { X = xStep * -i * 2, Y = (yStep * 2) * j*2 }, ClusterStep);

                    var downRight = new Bound2d(new Vector2Serialize { X = xStep * i * 2, Y = (yStep * 2) * -j*2 }, ClusterStep);
                    var downLeft = new Bound2d(new Vector2Serialize { X = xStep * -i * 2, Y = (yStep * 2) * -j*2 }, ClusterStep);

                    AddCluster(upRight);
                    AddCluster(upLeft);
                    AddCluster(downRight);
                    AddCluster(downLeft);
                }
            }
        }

        private void AddCluster(Bound2d bound2D)
        {
            //if (!Clusters.ContainsKey(bound2D))
            //    Clusters.Add(bound2D, new List<IEntity>(16));
        }
    }

    [Serializable, MessagePackObject]
    public struct Bound2d
    {
        [Key(0)]
        public Vector2Serialize Min;

        [Key(1)]
        public Vector2Serialize Max;

        [Key(2)]
        public Vector2Serialize Center;


        public Bound2d(Vector2Serialize center, Vector2Serialize size) : this()
        {
            Center = center;
            Min = new Vector2Serialize(center.X - size.X, center.Y - size.Y);
            Max = new Vector2Serialize(center.X + size.X, center.Y + size.Y);
        }

        public static Bound2d operator +(Bound2d currentBound, Vector2Serialize vector2) => currentBound.Encapsulate(vector2);

        public bool IsInsideBound(Vector3Serialize vector)
        {
            return vector.X > Min.X && vector.X < Max.X && vector.Z > Min.Y && vector.Z < Max.Y;
        }

        public void Encapsulate(Vector3Serialize vector3)
        {
            Encapsulate(new Vector2Serialize(vector3.X, vector3.Z));
        }

        public Bound2d Encapsulate(Vector2Serialize vector)
        {
            var max = new Vector2Serialize(Max.X + vector.X + Center.X, Max.Y + vector.Y + Center.Y);
            var min = new Vector2Serialize(Min.X - vector.X - Center.X, Min.Y - vector.Y - Center.Y);

            Min = min;
            Max = max;
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj is Bound2d d &&
                   Min.Equals(d.Min) &&
                   Max.Equals(d.Max) &&
                   Center.Equals(d.Center);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Min, Max, Center);
        }
    }
}
