using System.Globalization;
using Barotrauma.Items.Components;

namespace DockyardTools;

public static class Utils
{
    public static string FormatToDecimalPlace(this float value, int decimalPlaces) =>
        string.Format(new NumberFormatInfo() { NumberDecimalDigits = decimalPlaces }, "{0:F}", value);

    public static string FormatToDecimalPlace(this double value, int decimalPlaces) =>
        string.Format(new NumberFormatInfo() { NumberDecimalDigits = decimalPlaces }, "{0:F}", value);

    public static bool LowerInvariantNameIs(this Connection conn, string invariantname) =>
        conn.Name.ToLowerInvariant().Equals(invariantname.ToLowerInvariant());

    public static float Clamp(this float value, float min, float max) => Math.Clamp(value, min, max);
    
    public static float TrySetFloat(string s)
    {
        if (float.TryParse(s, out float v))
        {
            return v;
        }

        return 0f;
    }
        
    public static bool TrySetBool(string s)
    {
        if (bool.TryParse(s, out bool b1))
        {
            return b1;
        } 
        else if (float.TryParse(s, out float v))
        {
            return !v.Equals(0f);
        }

        return false;
    }
}