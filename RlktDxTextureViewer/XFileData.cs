using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RlktDxTextureViewer
{
 internal enum XFileType
    {
        NONE,
        BINARY,
        TEXT,
        BINARY_MSZIP,
    }
    internal class XFileData
    {
        public Dictionary<string, XTexture> textures = new Dictionary<string, XTexture>();
        internal string filename;

        public byte[] data { get; set; }
        public byte[] uncompData { get; set; } = null;

        public int texturePos { get; set; } = -1;
        XFileType fileType = XFileType.NONE;

        public void AddData(byte[] data)
        {
            if(uncompData == null)
            {
                uncompData = data;
            }
            else
            {
                byte[] curData = uncompData;

                uncompData = new byte[curData.Length + data.Length];
                Array.Copy(curData, 0, uncompData, 0, curData.Length);
                Array.Copy(data, 0, uncompData, curData.Length, data.Length);
            }
        }

        public void SaveToFile(string filename)
        {
            File.WriteAllBytes(filename, uncompData);
        }

        public string FindTexture(string filter)
        {
            int foundOffset = -1;
            byte[] pattern = Encoding.ASCII.GetBytes(filter);
            int maxFirstCharSlot = uncompData.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (uncompData[i] != pattern[0]) // Compare only first byte
                    continue;

                // Found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (uncompData[i + j] != pattern[j]) break;
                    if (j == 1)
                    {
                        foundOffset = i;
                        break;
                    }
                }
            }
            
            if(foundOffset != -1)
            {
                for(int i=foundOffset;i != 0;i--)
                {
                    if(uncompData[i] == 0)
                    {
                        int stringStart = i-3;
                        texturePos = stringStart;
                        using (BinaryReader br = new BinaryReader(new MemoryStream(uncompData)))
                        {
                            br.BaseStream.Position = stringStart;

                            int size = br.ReadInt32();
                            string texture = Encoding.ASCII.GetString( br.ReadBytes(size) );
                            return texture;
                        }
                    }
                }
            }

            return "NoTexture.tga";
        }

        public void SetTexture(string textureName)
        {
            MemoryStream outData = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(outData))
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(uncompData)))
                {
                    //Write the new buffer
                    bw.Write(br.ReadBytes(texturePos));

                    //Skip existing texture
                    br.BaseStream.Position = texturePos;
                    int size = br.ReadInt32();
                    br.BaseStream.Position += size;

                    //Write new texture name
                    bw.Write(textureName.Length);
                    bw.Write(Encoding.ASCII.GetBytes(textureName));

                    //Read to the end of stream
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        bw.Write(br.ReadByte());
                    }

                    //Set the uncomp data.
                    uncompData = outData.ToArray();
                }
            }
        }

        public byte[] GetData() => uncompData;
        public void SetFileType(XFileType type) => fileType = type;
        public XFileType GetFileType() => fileType;
    }
}
