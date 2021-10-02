using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WebcamTimelapseNET5
{
    static class Color
    {
        // Affine transforms for color spaces

        public static Matrix4x4 _8bitToFloat = new Matrix4x4(
            1f/255f, 0, 0, 0,
            0, 1f / 255f, 0, 0,
            0, 0, 1f / 255f, 0,
            0, 0, 0, 1);
        public static Matrix4x4 _floatTo8bit = new Matrix4x4(
            255f, 0, 0, 0,
            0, 255f, 0, 0,
            0, 0, 255f, 0,
            0, 0, 0, 1);

        // Source: https://mymusing.co/bt-709-yuv-to-rgb-conversion-color/
        public static Matrix4x4 RGBFloatToYPbPrFloatFullRange_1 = new Matrix4x4(
            0.2126f,0.7152f,0.0722f,0,
            -0.114572f,-0.385428f,0.5f,0,
            0.5f,-0.454153f,-0.045847f,0,
            0,0,0,1);
        public static Matrix4x4 RGBFloatToYPbPrFloatFullRange_2 = new Matrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0.5f,
            0, 0, 1, 0.5f,
            0, 0, 0, 1);

        // Source: https://mymusing.co/bt-709-yuv-to-rgb-conversion-color/
        public static Matrix4x4 YPbPrFloatToRGBFloatFullRange = new Matrix4x4(
            1f,0f,1.5748f,0,
            1f,-0.187324f,-0.468124f,0,
            1f,1.8556f,0f,0,
            0,0,0,1);

        // Source: https://mymusing.co/bt-709-yuv-to-rgb-conversion-color/
        public static Matrix4x4 RGBFloatToYCbCr8bitLimitedRange_1 = new Matrix4x4(
            0.2126f*219f,0.7152f * 219f, 0.0722f * 219f, 0,
            -0.114572f * 224f, -0.385428f * 224f, 0.5f * 224f, 0,
            0.5f * 224f, -0.454153f * 224f, -0.045847f * 224f, 0,
            0,0,0,1);
        public static Matrix4x4 RGBFloatToYCbCr8bitLimitedRange_2 = new Matrix4x4(
            1, 0, 0, 16,
            0, 1, 0, 128,
            0, 0, 1, 128,
            0,0,0,1);

        // From: https://web.archive.org/web/20180423091842/http://www.equasys.de/colorconversion.html
        public static Matrix4x4 YCbCr8BitLimitedRangeToRGB8bit_1 = new Matrix4x4(
            1, 0, 0, -16,
            0, 1, 0, -128,
            0, 0, 1, -128,
            0,0,0,1);
        public static Matrix4x4 YCbCr8BitLimitedRangeToRGB8bit_2 = new Matrix4x4(
            1.164f, 0, 1.793f, 0,
            1.164f, -0.213f, -0.533f, 0,
            1.164f, 2.112f, 0, 0,
            0,0,0,1);

        public static Matrix4x4 FixRGBConvertedAsPC709fromLimitedSource = _8bitToFloat* YCbCr8BitLimitedRangeToRGB8bit_2 * YCbCr8BitLimitedRangeToRGB8bit_1 * _floatTo8bit * RGBFloatToYPbPrFloatFullRange_2 * RGBFloatToYPbPrFloatFullRange_1 * _8bitToFloat;
        public static Matrix4x4 FixRGBConvertedAsPC709fromLimitedSource_transposed = Matrix4x4.Transpose(FixRGBConvertedAsPC709fromLimitedSource);
    }
}
