using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using UnityEditor;



[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SplitScreenBlueprint", order = 1)]
public class SplitScreenBlueprint : ScriptableObject
{

    #region DefualtState
    public const string RawDefualt = "AAEAAAD/////AQAAAAAAAAAMAgAAAD9VbmlTZW5zZSwgVmVyc2lvbj0wLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGwFAQAAABdTZXJpYWxpemFibGVSZWN0V3JhcHBlcgEAAAAFcmVjdHMEFFNlcmlhbGl6YWJsZVJlY3RbXVtdAgAAAAIAAAAJAwAAAAcDAAAAAQEAAAAQAAAABBJTZXJpYWxpemFibGVSZWN0W10CAAAACQQAAAAJBQAAAAkGAAAACQcAAAAJCAAAAAkJAAAACQoAAAAJCwAAAAkMAAAACQ0AAAAJDgAAAAkPAAAACRAAAAAJEQAAAAkSAAAACRMAAAAHBAAAAAABAAAAAQAAAAQQU2VyaWFsaXphYmxlUmVjdAIAAAAF7P///xBTZXJpYWxpemFibGVSZWN0BAAAAAF4AXkFd2lkdGgGaGVpZ2h0AAAAAAsLCwsCAAAAAAAAAAAAAAAAAIA/AACAPwcFAAAAAAEAAAACAAAABBBTZXJpYWxpemFibGVSZWN0AgAAAAHr////7P///wAAgL4AAAAAAAAAPwAAgD8B6v///+z///8AAIA+AAAAAAAAAD8AAIA/BwYAAAAAAQAAAAMAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAen////s////AAAAAAAAgD4AAIA/AAAAPwHo////7P///wAAgL4AAIC+AAAAPwAAAD8B5////+z///8AAIA+AACAvgAAAD8AAAA/BwcAAAAAAQAAAAQAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAeb////s////AACAvgAAgD4AAAA/AAAAPwHl////7P///wAAgD4AAIA+AAAAPwAAAD8B5P///+z///8AAIC+AACAvgAAAD8AAAA/AeP////s////AACAPgAAgL4AAAA/AAAAPwcIAAAAAAEAAAAFAAAABBBTZXJpYWxpemFibGVSZWN0AgAAAAHi////7P///wAAgL4jn5U+AAAAP7vB1D4B4f///+z///8AAIA+I5+VPgAAAD+7wdQ+AeD////s////rY2qvrvBVL6tjao+I58VPwHf////7P///wAAAAC7wVS+puSqPiOfFT8B3v///+z///8quao+u8FUvq2Nqj4jnxU/BwkAAAAAAQAAAAYAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAd3////s////rY2qvgAAgD6tjao+AAAAPwHc////7P///27zrTkAAIA+puSqPgAAAD8B2////+z///+m5Ko+AACAPq2Nqj4AAAA/Adr////s////rY2qvgAAgL6tjao+AAAAPwHZ////7P///27zrTkAAIC+puSqPgAAAD8B2P///+z///+m5Ko+AACAvq2Nqj4AAAA/BwoAAAAAAQAAAAcAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAdf////s////KrmqvgAAgD6tjao+AAAAPwHW////7P///wAAAAAAAIA+puSqPgAAAD8B1f///+z///8quao+AACAPq2Nqj4AAAA/AdT////s////bwDAvgAAgL4AAIA+AAAAPwHT////7P///98AAL4AAIC+AACAPgAAAD8B0v///+z///9C/v89AACAvgAAgD4AAAA/AdH////s////bwDAPgAAgL4AAIA+AAAAPwcLAAAAAAEAAAAIAAAABBBTZXJpYWxpemFibGVSZWN0AgAAAAHQ////7P///28AwL4AAIA+AACAPgAAAD8Bz////+z///9C/v+9AACAPgAAgD4AAAA/Ac7////s////3wAAPgAAgD4AAIA+AAAAPwHN////7P///28AwD4AAIA+AACAPgAAAD8BzP///+z///+R/7++AACAvgAAgD4AAAA/Acv////s////Qv7/vQAAgL4AAIA+AAAAPwHK////7P///98AAD4AAIC+AACAPgAAAD8Byf///+z///9vAMA+AACAvgAAgD4AAAA/BwwAAAAAAQAAAAkAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAcj////s////KrmqvpHPqj6tjao+3WCqPgHH////7P///wAAAACRz6o+puSqPt1gqj4Bxv///+z///8quao+kc+qPq2Nqj7dYKo+AcX////s////KrmqvgAAAACtjao+RT6rPgHE////7P///wAAAAAAAAAApuSqPkU+qz4Bw////+z///8quao+AAAAAK2Nqj5FPqs+AcL////s////KrmqvpHPqr6tjao+3WCqPgHB////7P///wAAAACRz6q+puSqPt1gqj4BwP///+z///8quao+kc+qvq2Nqj7dYKo+Bw0AAAAAAQAAAAoAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAb/////s////YKqqvu6pqj6rqqo+qaqqPgG+////7P///1HzLbPuqao+q6qqPqmqqj4Bvf///+z///9gqqo+7qmqPquqqj6pqqo+Abz////s////X6qqvpHPurarqqo+qaqqPgG7////7P///1s1GDYliry2q6qqPqmqqj4Buv///+z///9gqqo+JYq8tquqqj6pqqo+Abn////s////kf+/viSsqr4AAIA+qaqqPgG4////7P///0L+/70krKq+AACAPqmqqj4Bt////+z////fAAA+JKyqvgAAgD6pqqo+Abb////s////kf+/Pu6pqr4AAIA+qaqqPgcOAAAAAAEAAAALAAAABBBTZXJpYWxpemFibGVSZWN0AgAAAAG1////7P///2Cqqr7uqao+q6qqPqmqqj4BtP///+z///8AAAAA7amqPquqqj6pqqo+AbP////s////YKqqPu6pqj6rqqo+qaqqPgGy////7P///28AwL4AAAAAAACAPqmqqj4Bsf///+z////fAAC+JYq8tgAAgD6pqqo+AbD////s////Qv7/PSWKvLYAAIA+qaqqPgGv////7P///5H/vz4AAAAAAACAPqmqqj4Brv///+z///+R/7++7qmqvgAAgD6pqqo+Aa3////s////Qv7/ve2pqr4AAIA+qaqqPgGs////7P///0L+/z0krKq+AACAPqmqqj4Bq////+z///+R/78+JKyqvgAAgD6pqqo+Bw8AAAAAAQAAAAwAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAar////s////bwDAvu6pqj4AAIA+qaqqPgGp////7P///98AAL7uqao+AACAPqmqqj4BqP///+z///9C/v897amqPgAAgD6pqqo+Aaf////s////kf+/Pu2pqj4AAIA+qaqqPgGm////7P///28AwL4liry2AACAPqmqqj4Bpf///+z////fAAC+JYq8tgAAgD6pqqo+AaT////s////Qv7/PWxnPbcAAIA+qaqqPgGj////7P///28AwD4AAAAAAACAPqmqqj4Bov///+z///9vAMC+JKyqvgAAgD6pqqo+AaH////s////3wAAviSsqr4AAIA+qaqqPgGg////7P///0L+/z0krKq+AACAPqmqqj4Bn////+z///+R/78+JKyqvgAAgD6pqqo+BxAAAAAAAQAAAA0AAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAZ7////s////YKqqvgAAwD6rqqo+AACAPgGd////7P///1s1GDYAAMA+q6qqPgAAgD4BnP///+z///9gqqo+AADAPquqqj4AAIA+AZv////s////X6qqvgAAAD6rqqo+AACAPgGa////7P///1s1GDYAAAA+q6qqPgAAgD4Bmf///+z///8/q6o+AAAAPquqqj4AAIA+AZj////s////YKqqvgAAAL6rqqo+AACAPgGX////7P///wAAAAAAAAC+q6qqPgAAgD4Blv///+z///9dqqo+AAAAvquqqj4AAIA+AZX////s////bwDAvgAAwL4AAIA+AACAPgGU////7P///0L+/70AAMC+AACAPgAAgD4Bk////+z////fAAA+AADAvgAAgD4AAIA+AZL////s////kf+/PgAAwL4AAIA+AACAPgcRAAAAAAEAAAAOAAAABBBTZXJpYWxpemFibGVSZWN0AgAAAAGR////7P///2Cqqr4AAMA+q6qqPgAAgD4BkP///+z///9bNRg2AADAPquqqj4AAIA+AY/////s////P6uqPgAAwD6rqqo+AACAPgGO////7P///1+qqr4AAAA+q6qqPgAAgD4Bjf///+z///9R8y2zAAAAPquqqj4AAIA+AYz////s////PquqPgAAAD6rqqo+AACAPgGL////7P///5H/v74AAAC+AACAPgAAgD4Biv///+z///9C/v+9AAAAvgAAgD4AAIA+AYn////s////3wAAPgAAAL4AAIA+AACAPgGI////7P///28AwD4AAAC+AACAPgAAgD4Bh////+z///+R/7++AADAvgAAgD4AAIA+AYb////s////3wAAvgAAwL4AAIA+AACAPgGF////7P///0L+/z0AAMC+AACAPgAAgD4BhP///+z///9vAMA+AADAvgAAgD4AAIA+BxIAAAAAAQAAAA8AAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAYP////s////YKqqvgAAwD6rqqo+AACAPgGC////7P///wAAAAAAAMA+q6qqPgAAgD4Bgf///+z///9gqqo+AADAPquqqj4AAIA+AYD////s////kf+/vgAAAD4AAIA+AACAPgF/////7P///0L+/70AAAA+AACAPgAAgD4Bfv///+z////fAAA+AAAAPgAAgD4AAIA+AX3////s////bwDAPgAAAD4AAIA+AACAPgF8////7P///28AwL4AAAC+AACAPgAAgD4Be////+z///9C/v+9AAAAvgAAgD4AAIA+AXr////s////3wAAPgAAAL4AAIA+AACAPgF5////7P///5H/vz4AAAC+AACAPgAAgD4BeP///+z///+R/7++AADAvgAAgD4AAIA+AXf////s////Qv7/vQAAwL4AAIA+AACAPgF2////7P///98AAD4AAMC+AACAPgAAgD4Bdf///+z///9vAMA+AADAvgAAgD4AAIA+BxMAAAAAAQAAABAAAAAEEFNlcmlhbGl6YWJsZVJlY3QCAAAAAXT////s////bwDAvgAAwD4AAIA+AACAPgFz////7P///98AAL4AAMA+AACAPgAAgD4Bcv///+z///9C/v89AADAPgAAgD4AAIA+AXH////s////kf+/PgAAwD4AAIA+AACAPgFw////7P///28AwL4AAAA+AACAPgAAgD4Bb////+z////fAAC+AAAAPgAAgD4AAIA+AW7////s////Qv7/PQAAAD4AAIA+AACAPgFt////7P///28AwD4AAAA+AACAPgAAgD4BbP///+z///9vAMC+AAAAvgAAgD4AAIA+AWv////s////3wAAvgAAAL4AAIA+AACAPgFq////7P///0L+/z0AAAC+AACAPgAAgD4Baf///+z///+R/78+AAAAvgAAgD4AAIA+AWj////s////bwDAvgAAwL4AAIA+AACAPgFn////7P///0L+/70AAMC+AACAPgAAgD4BZv///+z////fAAA+AADAvgAAgD4AAIA+AWX////s////bwDAPgAAwL4AAIA+AACAPgs=";
    #endregion

    [SerializeField]
    public string RawString;

    public Rect[][] rects;



    [SerializeField]
    [HideInInspector]
    private byte[] rawBytes;




    public void Recover()
    {
        if (rawBytes == null) return;
        if (rawBytes.Length == 0) return;
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(rawBytes);

        SerializableRectWrapper wrapper = formatter.Deserialize(stream) as SerializableRectWrapper;
        rects = new Rect[wrapper.rects.Length][];
        for (int i = 0; i < wrapper.rects.Length; i++)
        {
            rects[i] = new Rect[wrapper.rects[i].Length];
            for (int j = 0; j < wrapper.rects[i].Length; j++)
            {
                rects[i][j] = wrapper.rects[i][j].ToRect();
            }
        }
    }

    public void Save()
    {
        SerializableRectWrapper wrapper = new SerializableRectWrapper();
        wrapper.rects = new SerializableRect[rects.Length][];
        for (int i = 0; i < rects.Length; i++)
        {
            wrapper.rects[i] = new SerializableRect[rects[i].Length];
            for (int j = 0; j < rects[i].Length; j++)
            {

                wrapper.rects[i][j] = new SerializableRect(rects[i][j]);
            }
        }

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream();
        formatter.Serialize(stream, wrapper);
        RawString = System.Convert.ToBase64String(stream.ToArray()); //Used for setting up the RawDefualt string up above
        rawBytes = stream.ToArray();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public Rect[][] RetriveDefualt(int Length = 16)
    {
        if (Length < 0) return null;
        if (Length > 16) return null;
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(System.Convert.FromBase64String(RawDefualt));
        SerializableRectWrapper wrapper = formatter.Deserialize(stream) as SerializableRectWrapper;
        Rect[][] rects = new Rect[wrapper.rects.Length][];
        for (int i = 0; i < wrapper.rects.Length; i++)
        {
            rects[i] = new Rect[wrapper.rects[i].Length];
            for (int j = 0; j < wrapper.rects[i].Length; j++)
            {
                rects[i][j] = wrapper.rects[i][j].ToRect();
            }
        }
        return rects;
    }

}

[System.Serializable]
public class SerializableRectWrapper
{
    public SerializableRect[][] rects;
}

[Serializable]
public struct SerializableRect
{
    public float x;
    public float y;
    public float width;
    public float height;

    public SerializableRect(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public SerializableRect(Rect rect)
    {
        x = rect.x;
        y = rect.y;
        width = rect.width;
        height = rect.height;
    }

    public Rect ToRect()
    {
        return new Rect(x, y, width, height);
    }
    public void FromRect(Rect rect)
    {
        x = rect.x;
        y = rect.y;
        width = rect.width;
        height = rect.height;
    }
}

