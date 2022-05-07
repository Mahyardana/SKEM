using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKEM
{
    public class FileDotEngine : IDotEngine
    {
        public string[] skems;
        public string Run(GraphvizImageType imageType, string dot, string outputFileName)
        {
            string output = outputFileName;

            for (int i = skems.Length - 1; i >= 0; i--)
            {
                dot = dot.Replace(i.ToString(), skems[i]);
            }

            File.WriteAllText(output, dot);

            // assumes dot.exe is on the path:
            var args = string.Format(@"{0} -Tjpg -O", output);
            System.Diagnostics.Process.Start("dot.exe", args);
            return output;
        }
    }
}
