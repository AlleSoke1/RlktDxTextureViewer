using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RlktDxTextureViewer
{
    internal class XException : Exception
    {
        public XException(string msg)
        {
            MessageBox.Show(msg);
        }
    }
}
