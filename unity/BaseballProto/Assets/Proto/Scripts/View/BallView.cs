using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 공 하나 (구체)
    /// </summary>
    public sealed class BallView
    {
        private readonly GameObject _ball;

        public BallView(Transform parent, float diameter)
        {
            _ball = PrimitiveFactory.Create(PrimitiveType.Sphere, "Ball", parent, Vector3.zero, Vector3.one * diameter,
                ProtoColors.Ball);
            _ball.SetActive(false);
        }

        public Vector3 Position => _ball.transform.localPosition;

        public void Show(Vector3 position)
        {
            _ball.SetActive(true);
            _ball.transform.localPosition = position;
        }

        public void Hide()
        {
            _ball.SetActive(false);
        }
    }
}
