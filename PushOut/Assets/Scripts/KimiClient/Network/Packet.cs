using UnityEngine;
using System;
/*
public static class PacketParser
{
    public static int ParseInt(JSONObject data)
    {
        int value = 0;
        if (data.type == JSONObject.Type.STRING)
        {
            string field = data.str;
            if (!int.TryParse(field, out value))
            {
                Debug.LogWarning("[PacketParser]Parse int fail! field : " + field);
            }
        }
        else if (data.type == JSONObject.Type.NUMBER)
        {
            value = Convert.ToInt32(data.n);
        }

        return value;
    }

    public static long ParseLong(JSONObject data)
    {
        long value = 0;
        if (data.type == JSONObject.Type.STRING)
        {
            string field = data.str;
            if (!long.TryParse(field, out value))
            {
                Debug.LogWarning("[PacketParser]Parse int fail! field : " + field);
            }
        }
        else if (data.type == JSONObject.Type.NUMBER)
        {
            value = Convert.ToInt64(data.n);
        }

        return value;
    }

    public static DateTime ParseDateTime(JSONObject data)
    {
        long value = 0;
        if (data.type == JSONObject.Type.STRING)
        {
            string field = data.str;
            if (!long.TryParse(field, out value))
            {
                Debug.LogWarning("[PacketParser]Parse int fail! field : " + field);
            }
        }
        else if(data.type == JSONObject.Type.NUMBER)
        {
            value = Convert.ToInt64(data.n);
        }

        return new DateTime(value, DateTimeKind.Utc);
    }

    public static float ParseFloat(JSONObject data)
    {
        float value = 0;
        if (data.type == JSONObject.Type.STRING)
        {
            string field = data.str;
            if (!float.TryParse(field, out value))
            {
                Debug.LogWarning("[PacketParser]Parse int fail! field : " + field);
            }
        }
        else if (data.type == JSONObject.Type.NUMBER)
        {
            value = Convert.ToSingle(data.n);
        }


        return value;
    }
}
*/