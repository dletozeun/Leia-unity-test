using System.Collections.Generic;
using UnityEngine;

namespace LeiaLoft
{
    public class DisplayConfigCalculator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="interlaced_width">Width of the interlaced image in pixels.</param>
        /// <param name="interlaced_height">Height of the interlaced image in pixels.</param>
        /// <param name="display_alignment_offset">
        /// </param>
        /// <param name="num_views">The number of views.</param>
        /// <param name="view_id_increases_with_x">
        /// Toggles wether the view id's increase from left to right or right to left.
        /// Flipping this parameter together with view_id_increases_with_channel effectively reverses the view order.
        /// </param>
        /// <param name="slant">Toggles wether view id's increase or decrease with y.</param>
        /// <param name="reverse_view_order">Should the view order be reversed.</param>
        /// <param name="mirror_x_per_view">Should each view be mirrored in x.</param>
        /// <param name="mirror_y_per_view">Should each view be mirrored in y</param>
        /// <param name="interlaceMatrix">The calculated interlace matrix.</param>
        /// <param name="interlaceVector">The calculated interlace vector.</param>
        public static void CalculateInterlaceMatrix(int interlaced_width,
                                                    int interlaced_height,
                                                    int display_alignment_offset,
                                                    int num_views,
                                                    bool slant,
                                                    bool reverse_view_order,
                                                    bool mirror_x_per_view,
                                                    bool mirror_y_per_view,
                                                    out Matrix4x4 interlace_matrix,
                                                    out Vector4 interlace_vector)
        {
            Matrix4x4 non_normalized_interlace_matrix = Matrix4x4.identity;
            Vector4 non_normalized_interlace_vector = Vector4.zero;

            Vector4 interlaced_resolution = new Vector4(interlaced_width, interlaced_height, 3, num_views);

            non_normalized_interlace_matrix.m30 = 3.0f;
            non_normalized_interlace_matrix.m31 = 1.0f;
            non_normalized_interlace_matrix.m32 = 1.0f;
            non_normalized_interlace_matrix.m33 = 0.0f;

            non_normalized_interlace_vector[3] = (float)display_alignment_offset;

            if (!slant)
            {
                non_normalized_interlace_matrix.m31 *= -1.0f;
            }

            if (reverse_view_order)
            {
                non_normalized_interlace_matrix.m30 *= -1.0f;
                non_normalized_interlace_matrix.m31 *= -1.0f;
                non_normalized_interlace_matrix.m32 *= -1.0f;
                non_normalized_interlace_vector[3] = num_views - 1.0f - non_normalized_interlace_vector[3];
            }

            if (mirror_x_per_view)
            {
                non_normalized_interlace_matrix.m00 = -1.0f;
                non_normalized_interlace_vector[0] = (float)interlaced_width - 1.0f;
            }

            if (mirror_y_per_view)
            {
                non_normalized_interlace_matrix.m11 = -1.0f;
                non_normalized_interlace_vector[1] = (float)interlaced_height - 1.0f;
            }

            PostMultiplyRGBAA(ref non_normalized_interlace_matrix, ref non_normalized_interlace_vector);

            // Normalize to unit cube.
            NormalizeInterlaceMatrix(interlaced_resolution, non_normalized_interlace_matrix, non_normalized_interlace_vector, out interlace_matrix, out interlace_vector);
        }

        static void NormalizeInterlaceMatrix(Vector4 interlace_resolution, Matrix4x4 non_normalized_matrix, Vector4 non_normalized_vector, out Matrix4x4 normalized_matrix, out Vector4 normalized_vector)
        {
            normalized_matrix = Matrix4x4.identity;
            normalized_vector = Vector4.zero;
            for (int i = 0; i < 4; ++i)
            {
                Vector4 non_normalized_row = non_normalized_matrix.GetRow(i);
                Vector4 normalized_row = Vector4.zero;
                for (int j = 0; j < 4; ++j)
                {
                    normalized_row[j] = non_normalized_row[j] * interlace_resolution[j] / interlace_resolution[i];
                }
                normalized_matrix.SetRow(i, normalized_row);
                normalized_vector[i] = non_normalized_vector[i] / interlace_resolution[i];
            }
        }

        static void PostMultiplyRGBAA(ref Matrix4x4 non_normalized_interlace_matrix, ref Vector4 non_normalized_interlace_vector)
        {
            // Calculate RBG anti-aliasing matrix that shifts red and blue components to sample from the same xy-position as their corresponding green component.
            Matrix4x4 rgb_antialiasing_matrix = Matrix4x4.identity;
            Vector4 rgb_antialiasing_vector = Vector4.zero;

            float min_magnitude = 999.0f;
            Vector2 green_to_blue = Vector2.zero;
            for (green_to_blue.x = -2; green_to_blue.x <= 2; ++green_to_blue.x)
            {
                green_to_blue.y = -green_to_blue.x * (non_normalized_interlace_matrix.m30 / non_normalized_interlace_matrix.m31) - (non_normalized_interlace_matrix.m32 / non_normalized_interlace_matrix.m31);
                if (isFloatAnInteger(green_to_blue.y, 0.001f))
                {
                    if (green_to_blue.magnitude < min_magnitude)
                    {
                        min_magnitude = green_to_blue.magnitude;
                        rgb_antialiasing_matrix.m02 = -green_to_blue.x;
                        rgb_antialiasing_matrix.m12 = -green_to_blue.y;
                        rgb_antialiasing_vector[0] = green_to_blue.x;
                        rgb_antialiasing_vector[1] = green_to_blue.y;
                    }
                }
            }

            // Post-multiply interlace matrix by rgb AA matrix.
            non_normalized_interlace_matrix = rgb_antialiasing_matrix * non_normalized_interlace_matrix;
            non_normalized_interlace_vector = rgb_antialiasing_matrix * non_normalized_interlace_vector + rgb_antialiasing_vector;
        }

        static bool isFloatAnInteger(float x, float epsilon)
        {
            return Mathf.Abs(x - Mathf.Round(x)) < epsilon;
        }

        public static List<float> convertActBetaToShaderCoeffs(List<float> ActCoefficients, float beta)
        {
            List<float> shaderCoeffs = new List<float>();
            shaderCoeffs.Add(1.0f);
            shaderCoeffs.AddRange(ActCoefficients);

            float normalizer = 1.0f;
            foreach (float a in ActCoefficients)
            {
                normalizer -= beta * a;
            }

            for (int i = 0; i < shaderCoeffs.Count; ++i)
            {
                shaderCoeffs[i] /= normalizer;
            }

            return shaderCoeffs;
        }
    }
}
