using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ZlibWithDictionary;

namespace RlktDxTextureViewer
{
    internal class Utils
    {
        XFileData xfile = new XFileData();
        public bool LoadXFile(string filename)
        {
            if (File.Exists(filename) == false)
                return false;

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
                    xfile.SetFileType(XFileType.BINARY);
                    return true;
                }

                if (strFormat == "txt ")
                {
                    xfile.AddData(xfile.data);
                    xfile.SetFileType(XFileType.TEXT);
                    return true;
                }

                //Binary MSZip Format
                if (strFormat != "bzip")
                    throw new XException(string.Format("Invalid file format ({0}), contact dev.", strFormat));

                xfile.SetFileType(XFileType.BINARY);

                reader.BaseStream.Seek(10, SeekOrigin.Current);

                //
                reader.BaseStream.Position = 0;
                byte[] header = reader.ReadBytes(16);
                header[8] = (byte)'b';
                header[9] = (byte)'i';
                header[10] = (byte)'n';
                header[11] = (byte)' ';
                xfile.AddData(header);

                reader.BaseStream.Seek(4, SeekOrigin.Current);

                int totalProcessedSize = 0;
                //
                byte[] previousByteArr = new byte[0];
                while (reader.BaseStream.Length != reader.BaseStream.Position)
                {
                    int uncompressSize = reader.ReadUInt16();
                    int nextSection = (int)reader.ReadUInt16();
                    int mszipheader = (int)reader.ReadUInt16(); //MSZIP Header 0x43 0x4B

                    if (mszipheader != 19267)
                    {
                        MessageBox.Show($"MSZIP Header not good {mszipheader}/ uncomp: {uncompressSize} / next: {nextSection}.");
                    }

                    totalProcessedSize = (int)reader.BaseStream.Position;

                    //reader.BaseStream.Position -= 2;
                    byte[] sectionData = reader.ReadBytes(nextSection-2); 

                    MemoryStream ms_in = new MemoryStream(sectionData);
                    MemoryStream ms_out = new MemoryStream();
                    Ionic.Zlib.DeflateStream deflateStream = new Ionic.Zlib.DeflateStream(ms_in, Ionic.Zlib.CompressionMode.Decompress);

                    var buffer = new byte[uncompressSize];

                    var codec = new ZlibCodec
                    {
                        InputBuffer = sectionData,
                        OutputBuffer = buffer,
                        NextIn = 0,
                        NextOut = 0,
                        AvailableBytesIn = sectionData.Length,
                        AvailableBytesOut = uncompressSize
                    };

                    codec.InitializeInflate(false);
                    try
                    {
                        while (true)
                        {
                            if (previousByteArr != null && previousByteArr.Length > 0)
                            {
                                codec.SetDictionaryUnconditionally(previousByteArr);
                            }

                            var inflateReturnCode = codec.Inflate(FlushType.None);
                            //var bytesToWrite = bufferSize - codec.AvailableBytesOut;
                            var bytesToWrite = uncompressSize;
                            ms_out.Write(buffer, 0, bytesToWrite);
                            if (inflateReturnCode == 0)
                                break;

                            if (inflateReturnCode == ZlibConstants.Z_STREAM_END)
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        //MessageBox.Show(e.Message);
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
