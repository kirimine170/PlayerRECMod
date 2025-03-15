using System.ComponentModel;
using AsmResolver.Patching;
using Il2CppSystem.Xml.Schema;
using MelonLoader;
using UnityEngine;

public class Program : MelonMod
{
    GameObject player;
    GameObject dummy;
    public override void OnInitializeMelon()
    {
    }
    public override void OnUpdate()
    {
        if (player == null)
            player = GameObject.Find("/Player/XR Origin");
        if (dummy == null)
            dummy = new GameObject("dummy");
        //Console.WriteLine("raw:" + player.transform.position.x + "," + player.transform.position.y + "," + player.transform.position.z);
        Console.WriteLine("ORIGINAL:" + player.transform.position.ToString() + player.transform.rotation.ToString());
        // Console.WriteLine("cov:" + BitConverter.ToString(PositionToBytes(player.transform.position)));
        Console.WriteLine("HEX:" + BitConverter.ToString(TransformToBytes(player.transform)));
        // Console.WriteLine("res:" + BytesToPosition(PositionToBytes(player.transform.position)).ToString());
        BytesToTransform(TransformToBytes(player.transform), dummy.transform);
        Console.WriteLine("CLONE:" + dummy.transform.position.ToString() + dummy.transform.rotation.ToString());

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Console.WriteLine("Testing...");
            // なんか書け
            Console.WriteLine("=====");
        }
    }

    public static byte[] TransformToBytes(Transform transform)
    {
        // position: 12バイト, rotation: 16バイト, localScale: 12バイト → 合計40バイト
        byte[] positionBytes = PositionToBytes(transform.position);
        byte[] rotationBytes = QuaternionToBytes(transform.rotation);
        byte[] scaleBytes = PositionToBytes(transform.localScale); // Vector3として扱う

        byte[] bytes = new byte[positionBytes.Length + rotationBytes.Length + scaleBytes.Length];
        Buffer.BlockCopy(positionBytes, 0, bytes, 0, positionBytes.Length);
        Buffer.BlockCopy(rotationBytes, 0, bytes, positionBytes.Length, rotationBytes.Length);
        Buffer.BlockCopy(scaleBytes, 0, bytes, positionBytes.Length + rotationBytes.Length, scaleBytes.Length);

        return bytes;
    }

    public static void BytesToTransform(byte[] bytes, Transform transform)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != 40)
            throw new ArgumentException("Byte配列は40バイトである必要があります。", nameof(bytes));

        // 先頭12バイト: position
        byte[] positionBytes = new byte[12];
        Buffer.BlockCopy(bytes, 0, positionBytes, 0, 12);
        Vector3 position = BytesToPosition(positionBytes);

        // 次の16バイト: rotation
        byte[] rotationBytes = new byte[16];
        Buffer.BlockCopy(bytes, 12, rotationBytes, 0, 16);
        Quaternion rotation = BytesToQuaternion(rotationBytes);

        // 残りの12バイト: localScale
        byte[] scaleBytes = new byte[12];
        Buffer.BlockCopy(bytes, 28, scaleBytes, 0, 12);
        Vector3 scale = BytesToPosition(scaleBytes);

        // Transformに反映
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
    }
    public static byte[] PositionToBytes(Vector3 position)
    {
        byte[] xBytes = BitConverter.GetBytes(position.x);
        byte[] yBytes = BitConverter.GetBytes(position.y);
        byte[] zBytes = BitConverter.GetBytes(position.z);

        byte[] bytes = new byte[xBytes.Length + yBytes.Length + zBytes.Length];
        Buffer.BlockCopy(xBytes, 0, bytes, 0, xBytes.Length);
        Buffer.BlockCopy(yBytes, 0, bytes, xBytes.Length, yBytes.Length);
        Buffer.BlockCopy(zBytes, 0, bytes, xBytes.Length + yBytes.Length, zBytes.Length);

        return bytes;
    }

    public static Vector3 BytesToPosition(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != 12)
            throw new ArgumentException("Byte配列は12バイトである必要があります。", nameof(bytes));

        float x = BitConverter.ToSingle(bytes, 0);
        float y = BitConverter.ToSingle(bytes, 4);
        float z = BitConverter.ToSingle(bytes, 8);

        return new Vector3(x, y, z);
    }

    public static byte[] QuaternionToBytes(Quaternion quaternion)
    {
        byte[] xBytes = BitConverter.GetBytes(quaternion.x);
        byte[] yBytes = BitConverter.GetBytes(quaternion.y);
        byte[] zBytes = BitConverter.GetBytes(quaternion.z);
        byte[] wBytes = BitConverter.GetBytes(quaternion.w);

        byte[] bytes = new byte[xBytes.Length + yBytes.Length + zBytes.Length + wBytes.Length];
        Buffer.BlockCopy(xBytes, 0, bytes, 0, xBytes.Length);
        Buffer.BlockCopy(yBytes, 0, bytes, xBytes.Length, yBytes.Length);
        Buffer.BlockCopy(zBytes, 0, bytes, xBytes.Length + yBytes.Length, zBytes.Length);
        Buffer.BlockCopy(wBytes, 0, bytes, xBytes.Length + yBytes.Length + zBytes.Length, wBytes.Length);

        return bytes;
    }

    public static Quaternion BytesToQuaternion(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != 16)
            throw new ArgumentException("Byte配列は16バイトである必要があります。", nameof(bytes));

        float x = BitConverter.ToSingle(bytes, 0);
        float y = BitConverter.ToSingle(bytes, 4);
        float z = BitConverter.ToSingle(bytes, 8);
        float w = BitConverter.ToSingle(bytes, 12);

        return new Quaternion(x, y, z, w);
    }

}