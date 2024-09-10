using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;
using Waher.Runtime.Console;

namespace Waher.Security.EllipticCurves.Test
{
    [TestClass]
    public class Arithmetic
    {
        [TestMethod]
        public void Test_01_Inverse()
        {
            PrimeFieldCurve C = new NistP256();
            int i;

            for (i = 0; i < 1000; i++)
            {
                BigInteger k = EllipticCurve.ToInt(C.GenerateSecret());
                BigInteger kInv = C.ModulusP.Invert(k);
                Assert.IsTrue(kInv >= BigInteger.One);
                Assert.IsTrue(kInv < C.Prime);

                BigInteger Mul = C.ModulusP.Multiply(k, kInv);

                Assert.IsTrue(Mul.IsOne);
            }
        }

        [TestMethod]
        public void Test_02_Negate()
        {
            WeierstrassCurve C = new NistP256();
            int i;

            for (i = 0; i < 100; i++)
            {
                byte[] k = C.GenerateSecret();
                PointOnCurve P = C.ScalarMultiplication(k, C.PublicKeyPoint, true);
                PointOnCurve Q = P;
                C.Negate(ref Q);
                C.AddTo(ref P, Q);
                Assert.IsFalse(P.NonZero);
            }
        }

        [TestMethod]
        public void Test_03_Addition()
        {
            PrimeFieldCurve C = new NistP256();
            byte[] k1, k2, k3;
            PointOnCurve P1, P2, P3;
            string s1, s2, s3;
            int i;

            for (i = 0; i < 100; i++)
            {
                k1 = C.GenerateSecret();
                P1 = C.ScalarMultiplication(k1, C.PublicKeyPoint, true);
                s1 = P1.ToString();

                do
                {
                    k2 = C.GenerateSecret();
                    P2 = C.ScalarMultiplication(k2, C.PublicKeyPoint, true);
                    s2 = P2.ToString();
                }
                while (k2 == k1);

                Assert.AreNotEqual(P2, P1);
                Assert.AreNotEqual(s2, s1);

                do
                {
                    k3 = C.GenerateSecret();
                    P3 = C.ScalarMultiplication(k3, C.PublicKeyPoint, true);
                    s3 = P3.ToString();
                }
                while (k3 == k1 || k3 == k2);

                Assert.AreNotEqual(P3, P1);
                Assert.AreNotEqual(s3, s1);
                Assert.AreNotEqual(P3, P2);
                Assert.AreNotEqual(s3, s2);

                PointOnCurve S1 = P1;
                C.AddTo(ref S1, P2);
                C.AddTo(ref S1, P3);

                PointOnCurve S2 = P2;
                C.AddTo(ref S2, P3);
                C.AddTo(ref S2, P1);

                PointOnCurve S3 = P3;
                C.AddTo(ref S3, P1);
                C.AddTo(ref S3, P2);

                Assert.AreEqual(S1, S2);
                Assert.AreEqual(S2, S3);

                Assert.AreEqual(s1, P1.ToString());
                Assert.AreEqual(s2, P2.ToString());
                Assert.AreEqual(s3, P3.ToString());
            }
        }

        [TestMethod]
        public void Test_04_ScalarMultiplication_NIST_P256()
        {
			TestScalarMultiplication(new NistP256());
        }

        private static void TestScalarMultiplication(PrimeFieldCurve C)
        {
            Random Rnd = new();

            int k1 = Rnd.Next(1000, 2000);
            int k2 = Rnd.Next(1000, 2000);
            int k3 = Rnd.Next(1000, 2000);

            PointOnCurve P1 = C.ScalarMultiplication(k1, C.PublicKeyPoint, true);
            PointOnCurve P2 = C.ScalarMultiplication(k2, C.PublicKeyPoint, true);
            PointOnCurve P3 = C.ScalarMultiplication(k3, C.PublicKeyPoint, true);
            PointOnCurve P = C.ScalarMultiplication(k1 + k2 + k3, C.PublicKeyPoint, true);
            C.AddTo(ref P1, P2);
            C.AddTo(ref P1, P3);

            P.Normalize(C);
            P1.Normalize(C);

            Assert.AreEqual(P, P1);

            P2 = C.Zero;
            k1 += k2;
            k1 += k3;

            while (k1-- > 0)
                C.AddTo(ref P2, C.PublicKeyPoint);

            P2.Normalize(C);

            Assert.AreEqual(P, P2);
        }

        [TestMethod]
        public void Test_05_Sqrt_83_Mod_673()
        {
			CalcSqrt(83, 673);
        }

        [TestMethod]
        public void Test_06_Sqrt_Minus_486664_Mod_P()
        {
            Curve25519 C = new();
			CalcSqrt(-486664, C.Prime);
        }

        private static void CalcSqrt(BigInteger N, BigInteger p)
        {
            BigInteger x = ModulusP.SqrtModP(N, p);
            BigInteger N1 = BigInteger.Remainder(x * x, p);

            Assert.IsTrue(BigInteger.Remainder(N - N1, p).IsZero);
        }

        [TestMethod]
        public void Test_07_Coordinates_25519()
        {
            MontgomeryCurve C = new Curve25519();

            PointOnCurve UV = C.BasePoint;
            PointOnCurve XY = C.ToXY(UV);

            Assert.AreEqual("15112221349535400772501151409588531511454012693041857206046113283949847762202", XY.X.ToString());
            Assert.AreEqual("46316835694926478169428394003475163141307993866256225615783033603165251855960", XY.Y.ToString());

            PointOnCurve UV2 = C.ToUV(XY);

            Assert.AreEqual(UV.X, UV2.X);
            Assert.AreEqual(UV.Y, UV2.Y);
        }

        [TestMethod]
        public void Test_08_Coordinates_448()
        {
            MontgomeryCurve C = new Curve448();

            PointOnCurve UV = C.BasePoint;
            PointOnCurve XY = C.ToXY(UV);

            Edwards448 C2 = new();
            PointOnCurve XY4 = C2.ScalarMultiplication(4, C2.BasePoint, true);  // 4-isogeny

            Assert.AreEqual(XY4.X, XY.X);
            Assert.AreEqual(XY4.Y, XY.Y);

            PointOnCurve UV2 = C.ToUV(XY);
            PointOnCurve UV4 = C.ScalarMultiplication(4, C.BasePoint, true);

            Assert.AreEqual(UV4.X, UV2.X);

            BigInteger V4 = C.CalcV(UV4.X);
            Assert.IsTrue(V4 == UV2.Y || (C.Prime - V4) == UV2.Y);
        }

        [TestMethod]
        public void Test_09_CalcBits()
        {
            Assert.AreEqual(0, ModulusP.CalcBits(0));
            Assert.AreEqual(1, ModulusP.CalcBits(1));
            Assert.AreEqual(2, ModulusP.CalcBits(2));
            Assert.AreEqual(2, ModulusP.CalcBits(3));
            Assert.AreEqual(3, ModulusP.CalcBits(4));
            Assert.AreEqual(3, ModulusP.CalcBits(5));
            Assert.AreEqual(3, ModulusP.CalcBits(6));
            Assert.AreEqual(3, ModulusP.CalcBits(7));
            Assert.AreEqual(8, ModulusP.CalcBits(255));
            Assert.AreEqual(9, ModulusP.CalcBits(256));
            Assert.AreEqual(16, ModulusP.CalcBits(65535));
            Assert.AreEqual(17, ModulusP.CalcBits(65536));
        }

        [TestMethod]
        public void Test_10_X25519_TestVector_1()
        {
            byte[] NBin = Hashes.StringToBinary("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4");
            NBin[0] &= 248;
            NBin[31] &= 127;
            NBin[31] |= 64;
            BigInteger N0 = new(NBin);
            BigInteger N = BigInteger.Parse("31029842492115040904895560451863089656472772604678260265531221036453811406496");
            Assert.AreEqual(N, N0);

            byte[] UBin = Hashes.StringToBinary("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
            BigInteger U0 = new(UBin);
            BigInteger U = BigInteger.Parse("34426434033919594451155107781188821651316167215306631574996226621102155684838");
            Assert.AreEqual(U, U0);

            byte[] NUBin = Hashes.StringToBinary("c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552");
            BigInteger NU0 = new(NUBin);
            MontgomeryCurve C = new Curve25519();
            BigInteger NU = C.ScalarMultiplication(N, U);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_11_X25519_TestVector_2()
        {
            byte[] A = Hashes.StringToBinary("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d");
            A[0] &= 248;
            A[31] &= 127;
            A[31] |= 64;
            BigInteger N0 = new(A);
            BigInteger N = BigInteger.Parse("35156891815674817266734212754503633747128614016119564763269015315466259359304");
            Assert.AreEqual(N, N0);

            A = Hashes.StringToBinary("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493");
            A[31] &= 127;
            BigInteger U0 = new(A);
            BigInteger U = BigInteger.Parse("8883857351183929894090759386610649319417338800022198945255395922347792736741");
            Assert.AreEqual(U, U0);

            A = Hashes.StringToBinary("95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957");
            BigInteger NU0 = new(A);
            MontgomeryCurve C = new Curve25519();
            BigInteger NU = C.ScalarMultiplication(N, U);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_12_X25519_TestVector_3_1()
        {
			X25519_TestVector_3(1, "422c8e7a6227d7bca1350b3e2bb7279f7897b87bb6854b783c60e80311ae3079");
        }

        [TestMethod]
        public void Test_13_X25519_TestVector_3_1000()
        {
			X25519_TestVector_3(1000, "684cf59ba83309552800ef566f2f4d3c1c3887c49360e3875f2eb94d99532c51");
        }

        [TestMethod]
        [Ignore]
        public void Test_14_X25519_TestVector_3_1000000()
        {
			X25519_TestVector_3(1000000, "7c3911e0ab2586fd864497297e575e6f3bc601c0883c30df5f4dd2d24f665424");
        }

        private static void X25519_TestVector_3(int i, string HexResult)
        {
            MontgomeryCurve C = new Curve25519();
            BigInteger N = 9;
            BigInteger U = 9;
            BigInteger NU = BigInteger.Zero;

            while (i-- > 0)
            {
                byte[] NBin = N.ToByteArray();
                if (NBin.Length != 32)
                    Array.Resize<byte>(ref NBin, 32);

                NBin[0] &= 0xf8;
                NBin[31] &= 0x3f;
                NBin[31] |= 0x40;

                NU = C.ScalarMultiplication(NBin, U);

                U = N;
                N = NU;
            }

            byte[] A = Hashes.StringToBinary(HexResult);
            BigInteger NU0 = new(A);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_15_X25519_ECDH()
        {
            byte[] A = Hashes.StringToBinary("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
            Curve25519 Alice = new(A);

            Assert.AreEqual("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a",
                Hashes.BinaryToString(Alice.PublicKey));

            A = Hashes.StringToBinary("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb");
            Curve25519 Bob = new(A);

            Assert.AreEqual("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f",
                Hashes.BinaryToString(Bob.PublicKey));

            byte[] Key1 = Alice.GetSharedKey(Bob.PublicKey, Hashes.ComputeSHA256Hash);
            byte[] Key2 = Bob.GetSharedKey(Alice.PublicKey, Hashes.ComputeSHA256Hash);

            string k1 = Hashes.BinaryToString(Key1);
            string k2 = Hashes.BinaryToString(Key2);
            Assert.AreEqual(k1, k2);

            A = Hashes.StringToBinary("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");
            if (A.Length != 32)
                Array.Resize<byte>(ref A, 32);

            Array.Reverse(A);   // Most significant byte first.

            A = Hashes.ComputeSHA256Hash(A);
            string k3 = Hashes.BinaryToString(A);
            Assert.AreEqual(k1, k3);
        }

        [TestMethod]
        public void Test_16_X448_TestVector_1()
        {
            byte[] A = Hashes.StringToBinary("3d262fddf9ec8e88495266fea19a34d28882acef045104d0d1aae121700a779c984c24f8cdd78fbff44943eba368f54b29259a4f1c600ad3");
            A[0] &= 252;
            A[55] |= 128;
            Array.Resize(ref A, 57);
            BigInteger N0 = new(A);
            BigInteger N = BigInteger.Parse("599189175373896402783756016145213256157230856085026129926891459468622403380588640249457727683869421921443004045221642549886377526240828");
            Assert.AreEqual(N, N0);

            A = Hashes.StringToBinary("06fce640fa3487bfda5f6cf2d5263f8aad88334cbd07437f020f08f9814dc031ddbdc38c19c6da2583fa5429db94ada18aa7a7fb4ef8a086");
            Array.Resize(ref A, 57);
            BigInteger U0 = new(A);
            BigInteger U = BigInteger.Parse("382239910814107330116229961234899377031416365240571325148346555922438025162094455820962429142971339584360034337310079791515452463053830");
            Assert.AreEqual(U, U0);

            A = Hashes.StringToBinary("ce3e4ff95a60dc6697da1db1d85e6afbdf79b50a2412d7546d5f239fe14fbaadeb445fc66a01b0779d98223961111e21766282f73dd96b6f");
            Array.Resize(ref A, 57);
            BigInteger NU0 = new(A);
            MontgomeryCurve C = new Curve448();
            BigInteger NU = C.ScalarMultiplication(N, U);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_17_X448_TestVector_2()
        {
            byte[] A = Hashes.StringToBinary("203d494428b8399352665ddca42f9de8fef600908e0d461cb021f8c538345dd77c3e4806e25f46d3315c44e0a5b4371282dd2c8d5be3095f");
            A[0] &= 252;
            A[55] |= 128;
            Array.Resize(ref A, 57);
            BigInteger N0 = new(A);
            BigInteger N = BigInteger.Parse("633254335906970592779259481534862372382525155252028961056404001332122152890562527156973881968934311400345568203929409663925541994577184");
            Assert.AreEqual(N, N0);

            A = Hashes.StringToBinary("0fbcc2f993cd56d3305b0b7d9e55d4c1a8fb5dbb52f8e9a1e9b6201b165d015894e56c4d3570bee52fe205e28a78b91cdfbde71ce8d157db");
            Array.Resize(ref A, 57);
            BigInteger U0 = new(A);
            BigInteger U = BigInteger.Parse("622761797758325444462922068431234180649590390024811299761625153767228042600197997696167956134770744996690267634159427999832340166786063");
            Assert.AreEqual(U, U0);

            A = Hashes.StringToBinary("884a02576239ff7a2f2f63b2db6a9ff37047ac13568e1e30fe63c4a7ad1b3ee3a5700df34321d62077e63633c575c1c954514e99da7c179d");
            Array.Resize(ref A, 57);
            BigInteger NU0 = new(A);
            MontgomeryCurve C = new Curve448();
            BigInteger NU = C.ScalarMultiplication(N, U);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_18_X448_TestVector_3_1()
        {
			X448_TestVector_3(1, "3f482c8a9f19b01e6c46ee9711d9dc14fd4bf67af30765c2ae2b846a4d23a8cd0db897086239492caf350b51f833868b9bc2b3bca9cf4113");
        }

        [TestMethod]
        public void Test_19_X448_TestVector_3_1000()
        {
			X448_TestVector_3(1000, "aa3b4749d55b9daf1e5b00288826c467274ce3ebbdd5c17b975e09d4af6c67cf10d087202db88286e2b79fceea3ec353ef54faa26e219f38");
        }

        [TestMethod]
        [Ignore]
        public void Test_20_X448_TestVector_3_1000000()
        {
			X448_TestVector_3(1000000, "077f453681caca3693198420bbe515cae0002472519b3e67661a7e89cab94695c8f4bcd66e61b9b9c946da8d524de3d69bd9d9d66b997e37");
        }

        private static void X448_TestVector_3(int i, string HexResult)
        {
            MontgomeryCurve C = new Curve448();
            BigInteger N = 5;
            BigInteger U = 5;
            BigInteger NU = BigInteger.Zero;

            while (i-- > 0)
            {
                byte[] NBin = N.ToByteArray();
                if (NBin.Length != 56)
                    Array.Resize<byte>(ref NBin, 56);

                NBin[0] &= 0xfc;
                NBin[55] |= 0x80;

                NU = C.ScalarMultiplication(NBin, U);

                U = N;
                N = NU;
            }

            byte[] A = Hashes.StringToBinary(HexResult);
            BigInteger NU0 = new(A);

            Assert.AreEqual(NU0, NU);
        }

        [TestMethod]
        public void Test_21_X448_ECDH()
        {
            byte[] A = Hashes.StringToBinary("9a8f4925d1519f5775cf46b04b5800d4ee9ee8bae8bc5565d498c28dd9c9baf574a9419744897391006382a6f127ab1d9ac2d8c0a598726b");
            Curve448 Alice = new(A);

            Assert.AreEqual("9b08f7cc31b7e3e67d22d5aea121074a273bd2b83de09c63faa73d2c22c5d9bbc836647241d953d40c5b12da88120d53177f80e532c41fa000",
                Hashes.BinaryToString(Alice.PublicKey));

            A = Hashes.StringToBinary("1c306a7ac2a0e2e0990b294470cba339e6453772b075811d8fad0d1d6927c120bb5ee8972b0d3e21374c9c921b09d1b0366f10b65173992d");
            Curve448 Bob = new(A);

            Assert.AreEqual("3eb7a829b0cd20f5bcfc0b599b6feccf6da4627107bdb0d4f345b43027d8b972fc3e34fb4232a13ca706dcb57aec3dae07bdc1c67bf33609",
                Hashes.BinaryToString(Bob.PublicKey));

            byte[] Key1 = Alice.GetSharedKey(Bob.PublicKey, Hashes.ComputeSHA256Hash);
            byte[] Key2 = Bob.GetSharedKey(Alice.PublicKey, Hashes.ComputeSHA256Hash);
            string k1 = Hashes.BinaryToString(Key1);
            string k2 = Hashes.BinaryToString(Key2);
            Assert.AreEqual(k1, k2);

            A = Hashes.StringToBinary("07fff4181ac6cc95ec1c16a94a0f74d12da232ce40a77552281d282bb60c0b56fd2464c335543936521c24403085d59a449a5037514a879d");
            if (A.Length != 56)
                Array.Resize<byte>(ref A, 56);

            Array.Reverse(A);   // Most significant byte first.

            A = Hashes.ComputeSHA256Hash(A);
            string k3 = Hashes.BinaryToString(A);
            Assert.AreEqual(k1, k3);
        }

        [TestMethod]
        public void Test_22_TwinCurves_25519()
        {
			EdwardsTwinTest<Edwards25519>(new Curve25519(), 1);
        }

        protected static void EdwardsTwinTest<CurveType>(MontgomeryCurve C1, int Isogeny)
            where CurveType : EdwardsCurveBase
        {
            int Ok = 0;
            int Error = 0;
            int i;

            for (i = 0; i < 100; i++)
            {
                try
                {
                    ConsoleOut.WriteLine(i);

                    CurveType C2 = C1.Pair as CurveType;

                    Assert.IsNotNull(C2);

                    PointOnCurve P1 = C1.PublicKeyPoint;
                    PointOnCurve P1_2 = C1.ToXY(P1);
                    PointOnCurve P2 = C2.PublicKeyPoint;

                    if (Isogeny != 1)
                        P2 = C2.ScalarMultiplication(Isogeny, P2, true);

                    Assert.AreEqual(P1_2.Y, P2.Y);

                    Ok++;
                }
                catch (Exception)
                {
                    Error++;
                }
            }

            Assert.AreEqual(0, Error);
        }

        [TestMethod]
        public void Test_23_TwinCurves_448()
        {
			EdwardsTwinTest<Edwards448>(new Curve448(), 4);
        }

        [TestMethod]
        public void Test_25_ScalarMultiplication_Edwards_25519()
        {
			TestScalarMultiplication(new Edwards25519());
        }

        [TestMethod]
        public void Test_26_ScalarMultiplication_Edwards_448()
        {
			TestScalarMultiplication(new Edwards448());
        }

        [TestMethod]
        public void Test_27_Sqrt_156324()
        {
            Curve448 C = new();
			CalcSqrt(156324, C.Prime);
        }

        [TestMethod]
        public void Test_28_Ed25519_EncodeDecode()
        {
			TestEncoding(new Edwards25519());
        }

        [TestMethod]
        public void Test_29_Ed448_EncodeDecode()
        {
			TestEncoding(new Edwards448());
        }

        private static void TestEncoding(EdwardsCurveBase Curve)
        {
            int i;
            int NrErrors = 0;

            for (i = 0; i < 100; i++)
            {
                PointOnCurve P1 = Curve.PublicKeyPoint;

                byte[] Encoded = EdDSA.Encode(P1, Curve);
                PointOnCurve P2 = EdDSA.Decode(Encoded, Curve);

                if (!P1.Equals(P2))
                    NrErrors++;
            }

            Assert.AreEqual(0, NrErrors);
        }

    }
}
