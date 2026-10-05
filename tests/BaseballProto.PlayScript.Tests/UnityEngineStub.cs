// Unity 스크립트를 Unity 밖에서 컴파일하기 위한 최소 UnityEngine 흉내 (연출 대본 테스트 전용)
using System;
namespace UnityEngine
{
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public float magnitude => (float)Math.Sqrt(x * x + y * y); }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 up => new Vector3(0, 1, 0);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public Vector3 normalized { get { float m = magnitude; return m > 1e-6f ? this / m : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => LerpUnclamped(a, b, Mathf.Clamp01(t));
        public static Vector3 MoveTowards(Vector3 c, Vector3 t, float d) { Vector3 v = t - c; float m = v.magnitude; return m <= d || m == 0 ? t : c + v / m * d; }
        public override string ToString() => $"({x:0.0},{y:0.0},{z:0.0})";
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Clamp(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
        public static int Clamp(int v, int a, int b) => Math.Max(a, Math.Min(b, v));
        public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Abs(float v) => Math.Abs(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-5f;
    }
}
