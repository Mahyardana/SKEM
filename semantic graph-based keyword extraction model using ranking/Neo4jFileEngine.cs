using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKEM
{
    public class Neo4jFileEngine
    {
        public static void GenerateCypher(string path, BidirectionalGraph<string, Edge<string>> graph)
        {
            StreamWriter sw = new StreamWriter(path);
            sw.WriteLine("CREATE ");
            var vertices = graph.Vertices.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                sw.WriteLine("(" + vertices[i] + ":Word {id:\"" + vertices[i] + "\"}),");
            }
            var edges = graph.Edges.ToArray();
            for (int i = 0; i < edges.Length; i++)
            {
                sw.Write("(" + edges[i].Source + ")-[:Relation]->(" + edges[i].Target + ")");
                if (i < edges.Length - 1)
                    sw.WriteLine(",");
                else
                    sw.WriteLine(';');
            }
            sw.Close();
        }
    }
}
