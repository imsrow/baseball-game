using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 공 하나 (구체)와 바닥 그림자.
    /// 그림자는 공 바로 아래 바닥에 둔다 (정오 그림자). 높이는 드러내지 않고 공이 얼마나 다가왔는지(깊이)를 보여준다
    /// </summary>
    public sealed class BallView
    {
        private const float ShadowThicknessM = 0.004f;

        // 마운드 윗면(약 0.03 m)보다 살짝 위. 카메라에서는 바닥에 붙어 보인다
        private const float ShadowHeightM = 0.035f;

        private readonly GameObject _ball;
        private readonly GameObject _shadow;

        public BallView(Transform parent, float diameter, float shadowDiameter)
        {
            _ball = PrimitiveFactory.Create(PrimitiveType.Sphere, "Ball", parent, Vector3.zero, Vector3.one * diameter,
                ProtoColors.Ball);
            _ball.SetActive(false);
            _shadow = PrimitiveFactory.Create(PrimitiveType.Sphere, "BallShadow", parent, Vector3.zero,
                new Vector3(shadowDiameter, ShadowThicknessM, shadowDiameter), ProtoColors.BallShadow);
            _shadow.SetActive(false);
        }

        public Vector3 Position => _ball.transform.localPosition;

        /// <summary>바닥 그림자 표시 여부 (FX 패널에서 켜고 끈다)</summary>
        public bool ShadowEnabled { get; set; } = true;

        public void Show(Vector3 position)
        {
            _ball.SetActive(true);
            _ball.transform.localPosition = position;
            _shadow.SetActive(ShadowEnabled);
            _shadow.transform.localPosition = new Vector3(position.x, ShadowHeightM, position.z);
        }

        public void Hide()
        {
            _ball.SetActive(false);
            _shadow.SetActive(false);
        }
    }
}
