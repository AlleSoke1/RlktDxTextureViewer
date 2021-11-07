using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZlibWithDictionary;

namespace RlktDxTextureViewer
{
    internal class Utils
    {
        XFileData xfile = new XFileData();
        public bool LoadXFile(string filename)
        {
            xfile.filename = filename;
            xfile.data = File.ReadAllBytes(filename);

            using (BinaryReader reader = new BinaryReader(new MemoryStream(xfile.data)))
            {
                char[] head = reader.ReadChars(3);
                string strHead = new string(head);
                if (strHead != "xof")
                    throw new XException("Invalid file. Not an .X object.");

                reader.BaseStream.Seek(5, SeekOrigin.Current);

                //Check file format
                char[] format = reader.ReadChars(4);
                string strFormat = new string(format);
                
                //Binary Format
                if(strFormat == "bin ")
                {
                    xfile.AddData(xfile.data);
                    return true;
                }
                
                //Binary MSZip Format
                if (strFormat.ToString() != "bzip")
                    throw new XException("Invalid file format, contact dev.");

                reader.BaseStream.Seek(10, SeekOrigin.Current);

                //
                reader.BaseStream.Position = 0;
                byte[] header = reader.ReadBytes(16);
                header[8] = (byte)'b';
                header[9] = (byte)'i';
                header[10] = (byte)'n';
                header[11] = (byte)' ';
                xfile.AddData(header);

                reader.BaseStream.Seek(6, SeekOrigin.Current);


                //
                byte[] previousByteArr = new byte[0];
                while (reader.BaseStream.Length != reader.BaseStream.Position)
                {
                    int nextSection = (int)reader.ReadUInt16();
                    reader.ReadUInt16();

                    byte[] sectionData = reader.ReadBytes(nextSection);

                    MemoryStream ms_in = new MemoryStream(sectionData);
                    MemoryStream ms_out = new MemoryStream();
                    Ionic.Zlib.DeflateStream deflateStream = new Ionic.Zlib.DeflateStream(ms_in, Ionic.Zlib.CompressionMode.Decompress);

                    const int bufferSize = 64*1024;
                    var buffer = new byte[bufferSize];

                    var codec = new ZlibCodec
                    {
                        InputBuffer = sectionData,
                        NextIn = 0,
                        AvailableBytesIn = sectionData.Length
                    };
                    codec.InitializeInflate(false);
                    codec.OutputBuffer = buffer;

                    while (true)
                    {
                        if (previousByteArr != null && previousByteArr.Length > 0)
                        {
                            codec.SetDictionaryUnconditionally(previousByteArr);
                        }

                        codec.NextOut = 0;
                        codec.AvailableBytesOut = bufferSize;
                        var inflateReturnCode = codec.Inflate(FlushType.None);
                        var bytesToWrite = bufferSize - codec.AvailableBytesOut;
                        ms_out.Write(buffer, 0, bytesToWrite);

                        if (inflateReturnCode == ZlibConstants.Z_STREAM_END)
                        {
                            break;
                        }
                    }

                    codec.EndInflate();

                    previousByteArr = ms_out.ToArray();
                    
                    xfile.AddData(previousByteArr);
                }

                //Save to file.
                //xfile.SaveToFile(filename + ".uncomp");
            }

            return true;
        }

        internal string GetFileName()
        {
            return Path.GetFileName(xfile.filename);
        }

        public bool SaveXFile(string outFilename)
        {
            xfile.SaveToFile(outFilename);

            return true;
        }

        public string GetTextureName()
        {
            return xfile.FindTexture(".tga");
        }

        public void SetTextureName(string textureName)
        {
            xfile.SetTexture(textureName);
        }
    }
}
