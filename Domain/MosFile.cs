using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Domain
{
    public record MosFile(string Link, string Filename, DateTime FileChanged);
}
