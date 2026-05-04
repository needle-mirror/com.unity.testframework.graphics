using System;

namespace UnityEngine.TestTools.Graphics.LegacyColorDifference
{
    struct DeltaEEvaluator
    {
        internal static Action TestHook { get; set; }

        internal static LegacyColorDifferencePixelResult EvaluatePixel(
            Color actual,
            Color expected,
            PixelEvaluationGuide evaluationGuide,
            LegacyColorDifferencePixelResult reportCard
        )
        {
            TestHook?.Invoke();

            reportCard.DeltaE = JabDeltaE(RgBtoJab(expected), RgBtoJab(actual));
            reportCard.DeltaEOverThreshold = Mathf.Max(0f, reportCard.DeltaE - evaluationGuide.DeltaEThreshold);
            if (evaluationGuide.EnabledModes.IsSet(PixelEvaluationModes.CountBadDeltaE))
            {
                reportCard.PixelIsCorrect &= reportCard.DeltaEOverThreshold <= 0;
            }

            return reportCard;
        }

        static float JabDeltaE(Vector3 v1, Vector3 v2)
        {
            var c1 = Mathf.Sqrt(v1.y * v1.y + v1.z * v1.z);
            var c2 = Mathf.Sqrt(v2.y * v2.y + v2.z * v2.z);

            var h1 = Mathf.Atan(v1.z / v1.y);
            var h2 = Mathf.Atan(v2.z / v2.y);

            var deltaH = 2f * Mathf.Sqrt(c1 * c2) * Mathf.Sin((h1 - h2) / 2f);
            var deltaE = Mathf.Sqrt(Mathf.Pow(v1.x - v2.x, 2f) + Mathf.Pow(c1 - c2, 2f) + deltaH * deltaH);
            return deltaE;
        }

        // sRGB to JzAzBz
        // https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272
        static Vector3 RgBtoJab(Color color)
        {
            var xyz = RgBtoXYZ(color.linear);

            const float kB = 1.15f;
            const float kG = 0.66f;
            const float kC1 = 0.8359375f; // 3424 / 2^12
            const float kC2 = 18.8515625f; // 2413 / 2^7
            const float kC3 = 18.6875f; // 2392 / 2^7
            const float kN = 0.15930175781f; // 2610 / 2^14
            const float kP = 134.034375f; // 1.7 * 2523 / 2^5
            const float kD = -0.56f;
            const float kD0 = 1.6295499532821566E-11f;

            var x2 = kB * xyz.x - (kB - 1f) * xyz.z;
            var y2 = kG * xyz.y - (kG - 1f) * xyz.x;

            var l = 0.41478372f * x2 + 0.579999f * y2 + 0.0146480f * xyz.z;
            var m = -0.2015100f * x2 + 1.120649f * y2 + 0.0531008f * xyz.z;
            var s = -0.0166008f * x2 + 0.264800f * y2 + 0.6684799f * xyz.z;
            l = Mathf.Pow(l / 10000f, kN);
            m = Mathf.Pow(m / 10000f, kN);
            s = Mathf.Pow(s / 10000f, kN);

            // Can we switch to unity.mathematics yet?
            var lms = new Vector3(l, m, s);
            var a = new Vector3(kC1, kC1, kC1) + kC2 * lms;
            var b = Vector3.one + kC3 * lms;
            var tmp = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

            lms.x = Mathf.Pow(tmp.x, kP);
            lms.y = Mathf.Pow(tmp.y, kP);
            lms.z = Mathf.Pow(tmp.z, kP);

            var jab = new Vector3(
                0.5f * lms.x + 0.5f * lms.y,
                3.524000f * lms.x + -4.066708f * lms.y + 0.542708f * lms.z,
                0.199076f * lms.x + 1.096799f * lms.y + -1.295875f * lms.z
            );

            jab.x = ((1f + kD) * jab.x) / (1f + kD * jab.x) - kD0;

            return jab;
        }

        // Linear RGB to XYZ using D65 ref. white
        static Vector3 RgBtoXYZ(Color color)
        {
            var x = color.r * 0.4124564f + color.g * 0.3575761f + color.b * 0.1804375f;
            var y = color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.0721750f;
            var z = color.r * 0.0193339f + color.g * 0.1191920f + color.b * 0.9503041f;
            return new Vector3(x * 100f, y * 100f, z * 100f);
        }
    }
}
