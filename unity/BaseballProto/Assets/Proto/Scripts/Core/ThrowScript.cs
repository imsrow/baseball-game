using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>연출 대본의 송구 한 번 (카메라가 받는 곳을 함께 잡는 데 쓴다)</summary>
    public sealed class ThrowScript
    {
        public float Start { get; set; }

        public float Arrive { get; set; }

        public Vector3 Target { get; set; }
    }
}
