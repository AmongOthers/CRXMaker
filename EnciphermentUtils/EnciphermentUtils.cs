using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;

public class EnciphermentUtils
{
    public const string ENCRYPT_POSTFIX = ".lx";
    private static readonly byte[] DEFAULT_DES_KEY = { 29, 61, 50, 151, 89, 238, 198, 204 };
    private static readonly byte[] DEFAULT_DES_IV = { 43, 134, 28, 227, 186, 0, 193, 127 };
    private static readonly EnciphermentUtils DEFAULT_ENCIPHERMENT_UTILS = new EnciphermentUtils(DEFAULT_DES_KEY, DEFAULT_DES_IV);

    private byte[] mDesKey;
    private byte[] mDesIV;
    public EnciphermentUtils(byte[] desKey, byte[] desIV)
    {
        mDesKey = desKey;
        mDesIV = desIV;
    }

    public static bool EncryptDir(String inName, String outName)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.encryptDir(inName, outName);
    }
    public bool encryptDir(String inName, String outName)
    {
        String inName_ = inName + "\\";
        if (outName.StartsWith(inName_))
        {
            return false;
        }
        if (!Directory.Exists(inName))
        {
            return false;
        }
        try
        {
            if (Directory.Exists(outName))
            {
                Directory.Delete(outName);
            }
        }
        catch
        {
            return false;
        }
        Directory.CreateDirectory(outName);

        String[] paths = Directory.GetDirectories(inName);
        foreach (String path in paths)
        {
            int index = path.LastIndexOf("\\");
            String sub_path = path.Substring(index);
            EncryptDir(path, outName + sub_path);
        }

        String[] files = Directory.GetFiles(inName);
        foreach (String file_name in files)
        {
            int index = file_name.LastIndexOf("\\");
            String sub_file_name = file_name.Substring(index);
            EncryptData(file_name, outName + sub_file_name + ".lx");
        }
        return true;
    }

    public static bool EncryptData(String inName, String outName)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.encryptData(inName, outName);
    }
    public bool encryptBytes(byte[] inBytes, out byte[] outBytes)
    {
        using (MemoryStream fin = new MemoryStream(inBytes),
            fout = new MemoryStream())
        {
            fout.SetLength(0);
            DES des = new DESCryptoServiceProvider();
            des.BlockSize = 64;
            using (CryptoStream encStream = new CryptoStream(fout, des.CreateEncryptor(mDesKey, mDesIV), CryptoStreamMode.Write))
            {
                try
                {
                    encStream.Write(inBytes, 0, inBytes.Length);
                    encStream.FlushFinalBlock();
                    outBytes = fout.ToArray();
                }
                catch
                {
                    outBytes = null;
                }
            }
        }
        return outBytes != null;
    }

    public bool encryptData(String inName, String outName)
    {

        //Create the file streams to handle the input and output files.
        FileStream fin = null;
        FileStream fout = null;

        try
        {
            fin = new FileStream(inName, FileMode.Open, FileAccess.Read);

            fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write);
        }
        catch (System.Exception)
        {
            if (fin != null)
            {
                fin.Close();
            }
            if (fout != null)
            {
                fout.Close();
            }
            return false;
        }

        fout.SetLength(0);



        //Create variables to help with read and write.

        byte[] bin = new byte[100]; //This is intermediate storage for the encryption.

        long rdlen = 0;              //This is the total number of bytes written.

        long totlen = fin.Length;    //This is the total length of the input file.

        int len;                     //This is the number of bytes to be written at a time.



        DES des = new DESCryptoServiceProvider();
        CryptoStream encStream = null;
        try
        {
            encStream = new CryptoStream(fout, des.CreateEncryptor(mDesKey, mDesIV), CryptoStreamMode.Write);
            while (rdlen < totlen)
            {

                len = fin.Read(bin, 0, 100);

                encStream.Write(bin, 0, len);

                rdlen = rdlen + len;

            }
        }
        catch (System.Exception)
        {
            if (encStream != null)
            {
                encStream.Close();
            }
            fout.Close();
            fin.Close();
            return false;
        }

        //Read from the input file, then encrypt and write to the output file.

        encStream.Close();
        fout.Close();
        fin.Close();

        return true;

    }

    public static bool DecryptDir(String inName, String outName)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.decryptDir(inName, outName);
    }
    public bool decryptDir(String inName, String outName)
    {

        try
        {
            String inName_ = inName + "\\";
            if (outName.StartsWith(inName_))
            {
                return false;
            }
            if (!Directory.Exists(inName))
            {
                return false;
            }
            try
            {
                if (Directory.Exists(outName))
                {
                    Directory.Delete(outName);
                }
            }
            catch
            {
                return false;
            }
            Directory.CreateDirectory(outName);


            String[] paths = Directory.GetDirectories(inName);
            foreach (String path in paths)
            {
                int index = path.LastIndexOf("\\");
                String sub_path = path.Substring(index);
                DecryptDir(path, outName + sub_path);
            }

            String[] files = Directory.GetFiles(inName);
            foreach (String file_name in files)
            {
                int index = file_name.LastIndexOf("\\");
                String sub_file_name = file_name.Substring(index);
                sub_file_name = sub_file_name.Substring(0, sub_file_name.Length - 3);
                DecryptData(file_name, outName + sub_file_name);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool DecryptData(String inName, String outName)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.decryptData(inName, outName);
    }
    public bool decryptBytes(byte[] inBytes, out byte[] outBytes)
    {
        using (MemoryStream fout = new MemoryStream())
        {
            fout.SetLength(0);
            DES des = new DESCryptoServiceProvider();
            using (CryptoStream encStream = new CryptoStream(fout, des.CreateDecryptor(mDesKey, mDesIV), CryptoStreamMode.Write))
            {
                try
                {
                    encStream.Write(inBytes, 0, inBytes.Length);
                    encStream.FlushFinalBlock();
                    outBytes = fout.ToArray();
                }
                catch
                {
                    outBytes = null;
                }
            }

        }
        return outBytes != null;
    }
    public bool decryptData(String inName, String outName)
    {
        try
        {
            //Create the file streams to handle the input and output files.
            FileStream fin = null;
            FileStream fout = null;

            try
            {
                fin = new FileStream(inName, FileMode.Open, FileAccess.Read);
                string outPath = Path.GetDirectoryName(outName);
                if (!string.IsNullOrEmpty(outPath) && !Directory.Exists(outPath))
                {
                    Directory.CreateDirectory(outPath);
                }
                fout = new FileStream(outName, FileMode.OpenOrCreate, FileAccess.Write);
            }
            catch (System.Exception e)
            {
                if (fin != null)
                {
                    fin.Close();
                }
                if (fout != null)
                {
                    fout.Close();
                }
                return false;
            }

            fout.SetLength(0);



            //Create variables to help with read and write.

            byte[] bin = new byte[100]; //This is intermediate storage for the encryption.

            long rdlen = 0;              //This is the total number of bytes written.

            long totlen = fin.Length;    //This is the total length of the input file.

            int len;                     //This is the number of bytes to be written at a time.



            DES des = new DESCryptoServiceProvider();

            CryptoStream encStream = null;
            try
            {

                encStream = new CryptoStream(fout, des.CreateDecryptor(mDesKey, mDesIV), CryptoStreamMode.Write);

                //Read from the input file, then encrypt and write to the output file.

                while (rdlen < totlen)
                {

                    len = fin.Read(bin, 0, 100);

                    encStream.Write(bin, 0, len);

                    rdlen = rdlen + len;
                }

            }
            catch (System.Exception e)
            {
                Logger.Logger.GetLogger(this).Error("解密错误2", e);
                if (encStream != null)
                {
                    encStream.Close();
                }
                fout.Close();
                fin.Close();
                return false;
            }



            encStream.Close();
            fout.Close();
            fin.Close();

            return true;
        }
        catch(Exception e)
        {
            Logger.Logger.GetLogger(this).Error("解密错误3", e);
            return false;
        }
    }

    public byte[] decBytesPlusBase64(byte[] inBytes)
    {
        byte[] temp = Convert.FromBase64String(Encoding.ASCII.GetString(inBytes));
        byte[] outBytes;
        bool result = decryptBytes(temp, out outBytes);
        if (result)
        {
            return outBytes;
        }
        else
        {
            return null;
        }
    }

    public static string DecStringPlusBase64(string inStr)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.decStringPlusBase64(inStr);
    }

    public static string EncStringPlusBase64(string inStr)
    {
        return DEFAULT_ENCIPHERMENT_UTILS.encStringPlusBase64(inStr);
    }

    public string decStringPlusBase64(string inStr)
    {
        byte[] outBytes = decBytesPlusBase64(Encoding.ASCII.GetBytes(inStr));
        return outBytes == null ? null : Encoding.UTF8.GetString(outBytes);
    }

    public string encStringPlusBase64(string inStr)
    {
        byte[] inBytes = Encoding.UTF8.GetBytes(inStr);
        byte[] outBytes;
        bool result = encryptBytes(inBytes, out outBytes);
        if (result)
        {
            return Convert.ToBase64String(outBytes);
        }
        else
        {
            return null;
        }
    }

    public string encStringPlusBase64AndHeader(string inStr)
    {
        string result = encStringPlusBase64(inStr);
        return result == null ? null : "abcd1234" + result;
    }
}
