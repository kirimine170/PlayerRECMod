using System.ComponentModel;
using AsmResolver.Patching;
using Il2CppSystem.Xml.Schema;
using MelonLoader;
using UnityEngine;
using System.Text;
using System.IO;

public class Program : MelonMod
{
    GameObject player;
    GameObject dummy;
    bool isRecording = false;
    float recordStartTime = 0f;
    List<string> recordedSamples = new List<string>();
    bool isReplaying = false;
    int replayIndex = 0;
    List<byte[]> replayData = new List<byte[]>();
    public override void OnInitializeMelon()
    {
    }
    public override void OnUpdate()
    {
        if (player == null)
            player = GameObject.Find("/Player/XR Origin");
        if (dummy == null)
            dummy = new GameObject("dummy");
        
        // F1キー押下で記録開始
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (!isRecording)
            {
                isRecording = true;
                recordStartTime = Time.time;
                recordedSamples.Clear();
                Console.WriteLine("時系列データの記録を開始しました（30秒間）...");
            }
        }

        // F2キーで再生開始
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (!isReplaying)
            {
                if (File.Exists("TimeSeriesData.txt"))
                {
                    string[] lines = File.ReadAllLines("TimeSeriesData.txt");
                    replayData.Clear();
                    foreach (string line in lines)
                    {
                        // 各行は連結された16進数表記（player.transformの場合は80文字）
                        if (line.Length != 80)
                        {
                            Console.WriteLine("無効なデータ形式の行をスキップ: " + line);
                            continue;
                        }
                        byte[] bytes = new byte[40];
                        for (int i = 0; i < 40; i++)
                        {
                            string byteString = line.Substring(i * 2, 2);
                            bytes[i] = byte.Parse(byteString, System.Globalization.NumberStyles.HexNumber);
                        }
                        replayData.Add(bytes);
                    }
                    if (replayData.Count > 0)
                    {
                        isReplaying = true;
                        replayIndex = 0;
                        Console.WriteLine("再生を開始します。");
                    }
                    else
                    {
                        Console.WriteLine("再生するデータがありません。");
                    }
                }
                else
                {
                    Console.WriteLine("TimeSeriesData.txtが見つかりません。");
                }
            }
        }
        
        // 記録中は毎フレームデータを取得してリストに追加
        if (isRecording)
        {
            // プレイヤーのTransformをbyte配列（40バイト）に変換
            byte[] transformBytes = TransformToBytes(player.transform);
            
            // byte配列を16進数表記に変換（各バイトの区切りは入れず、連続した文字列にする）
            StringBuilder sb = new StringBuilder(transformBytes.Length * 2);
            foreach (byte b in transformBytes)
            {
                sb.Append(b.ToString("X2"));
            }
            // 1サンプル＝1行としてリストに追加
            recordedSamples.Add(sb.ToString());
            
            // 30秒経過したら記録終了しファイルに保存
            if (Time.time - recordStartTime >= 30f)
            {
                File.WriteAllLines("TimeSeriesData.txt", recordedSamples);
                Console.WriteLine("時系列データの記録が終了し、TimeSeriesData.txtに保存されました。");
                isRecording = false;
            }
        }

        // 再生中の場合、保存済みデータを1サンプルずつdummyのTransformへ反映
        if (isReplaying)
        {
            if (replayIndex < replayData.Count)
            {
                byte[] currentBytes = replayData[replayIndex];
                BytesToTransform(currentBytes, player.transform);
                replayIndex++;
            }
            else
            {
                Console.WriteLine("再生が完了しました。");
                isReplaying = false;
            }
        }

        // データ確認用
        //Console.WriteLine("raw:" + player.transform.position.x + "," + player.transform.position.y + "," + player.transform.position.z);
        // Console.WriteLine("cov:" + BitConverter.ToString(PositionToBytes(player.transform.position)));
        // Console.WriteLine("res:" + BytesToPosition(PositionToBytes(player.transform.position)).ToString());
        
        // データ確認用（見やすい版）
        // Console.WriteLine("ORIGINAL:" + player.transform.position.ToString() + player.transform.rotation.ToString());
        // Console.WriteLine("HEX:" + BitConverter.ToString(TransformToBytes(player.transform)));
        // BytesToTransform(TransformToBytes(player.transform), dummy.transform);
        // Console.WriteLine("CLONE:" + dummy.transform.position.ToString() + dummy.transform.rotation.ToString());
    }

    public static byte[] TransformToBytes(Transform transform)
    {
        // position: 12バイト, rotation: 16バイト, localScale: 12バイト → 合計40バイト
        byte[] positionBytes = Vector3ToBytes(transform.position);
        byte[] rotationBytes = QuaternionToBytes(transform.rotation);
        byte[] scaleBytes = Vector3ToBytes(transform.localScale); // Vector3として扱う

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
        Vector3 position = BytesToVector3(positionBytes);

        // 次の16バイト: rotation
        byte[] rotationBytes = new byte[16];
        Buffer.BlockCopy(bytes, 12, rotationBytes, 0, 16);
        Quaternion rotation = BytesToQuaternion(rotationBytes);

        // 残りの12バイト: localScale
        byte[] scaleBytes = new byte[12];
        Buffer.BlockCopy(bytes, 28, scaleBytes, 0, 12);
        Vector3 scale = BytesToVector3(scaleBytes);

        // Transformに反映
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
    }
    public static byte[] Vector3ToBytes(Vector3 position)
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

    public static Vector3 BytesToVector3(byte[] bytes)
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