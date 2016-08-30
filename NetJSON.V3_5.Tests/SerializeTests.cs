using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace NetJSON.V3_5.Tests {
    [TestClass]
    public class SerializeTests {
        [TestMethod]
        public void CanSerializeSimpleObject() {
            var simple = new SimpleObject { MyInt = 1000, MyString = "Hello World" };

            var json = NetJSON.Serialize(simple);
            var simple2 = NetJSON.Deserialize<SimpleObject>(json);
        }

        [TestMethod]
        public void SerializeVector3Struct() {
            var value = new Vector3(10, 20, 30);
            var json = NetJSON.Serialize(value);
            var value2 = NetJSON.Deserialize<Vector3>(json);
        }

    }


    public struct Vector3 {
        public const float kEpsilon = 1E-05f;
        public float x;
        public float y;
        public float z;
        public float this[int index] {
            get {
                switch (index) {
                    case 0:
                        return this.x;
                    case 1:
                        return this.y;
                    case 2:
                        return this.z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
            set {
                switch (index) {
                    case 0:
                        this.x = value;
                        break;
                    case 1:
                        this.y = value;
                        break;
                    case 2:
                        this.z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }
       
        public float sqrMagnitude {
            get {
                return this.x * this.x + this.y * this.y + this.z * this.z;
            }
        }
        public static Vector3 zero {
            get {
                return new Vector3(0f, 0f, 0f);
            }
        }
        public static Vector3 one {
            get {
                return new Vector3(1f, 1f, 1f);
            }
        }
        public static Vector3 forward {
            get {
                return new Vector3(0f, 0f, 1f);
            }
        }
        public static Vector3 back {
            get {
                return new Vector3(0f, 0f, -1f);
            }
        }
        public static Vector3 up {
            get {
                return new Vector3(0f, 1f, 0f);
            }
        }
        public static Vector3 down {
            get {
                return new Vector3(0f, -1f, 0f);
            }
        }
        public static Vector3 left {
            get {
                return new Vector3(-1f, 0f, 0f);
            }
        }
        public static Vector3 right {
            get {
                return new Vector3(1f, 0f, 0f);
            }
        }
        [Obsolete("Use Vector3.forward instead.")]
        public static Vector3 fwd {
            get {
                return new Vector3(0f, 0f, 1f);
            }
        }
        public Vector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(float x, float y) {
            this.x = x;
            this.y = y;
            this.z = 0f;
        }
       
        
        
        public void Set(float new_x, float new_y, float new_z) {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
        }
        public static Vector3 Scale(Vector3 a, Vector3 b) {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
        public void Scale(Vector3 scale) {
            this.x *= scale.x;
            this.y *= scale.y;
            this.z *= scale.z;
        }
        public static Vector3 Cross(Vector3 lhs, Vector3 rhs) {
            return new Vector3(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
        }
        public override int GetHashCode() {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
        }
        public override bool Equals(object other) {
            if (!(other is Vector3)) {
                return false;
            }
            Vector3 vector = (Vector3)other;
            return this.x.Equals(vector.x) && this.y.Equals(vector.y) && this.z.Equals(vector.z);
        }
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) {
            return -2f * Vector3.Dot(inNormal, inDirection) * inNormal + inDirection;
        }
        
        public override string ToString() {
            return String.Format("({0:F1}, {1:F1}, {2:F1})", new object[]
			{
				this.x,
				this.y,
				this.z
			});
        }
        public string ToString(string format) {
            return String.Format("({0}, {1}, {2})", new object[]
			{
				this.x.ToString(format),
				this.y.ToString(format),
				this.z.ToString(format)
			});
        }
        public static float Dot(Vector3 lhs, Vector3 rhs) {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }
        

        public static float SqrMagnitude(Vector3 a) {
            return a.x * a.x + a.y * a.y + a.z * a.z;
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Vector3 operator -(Vector3 a, Vector3 b) {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Vector3 operator -(Vector3 a) {
            return new Vector3(-a.x, -a.y, -a.z);
        }
        public static Vector3 operator *(Vector3 a, float d) {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }
        public static Vector3 operator *(float d, Vector3 a) {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }
        public static Vector3 operator /(Vector3 a, float d) {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }
        public static bool operator ==(Vector3 lhs, Vector3 rhs) {
            return Vector3.SqrMagnitude(lhs - rhs) < 9.99999944E-11f;
        }
        public static bool operator !=(Vector3 lhs, Vector3 rhs) {
            return Vector3.SqrMagnitude(lhs - rhs) >= 9.99999944E-11f;
        }
    }

    public class SimpleObject {
        public string MyString { get; set; }
        public int MyInt { get; set; }
    }
}
