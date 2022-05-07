// See https://aka.ms/new-console-template for more information
using MongoDB.Driver;
using QuickGraph;
using QuickGraph.Graphviz;
using SKEMCollections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;


var client = new MongoClient();
var db = client.GetDatabase("SKEMDB");
var tweets = db.GetCollection<Tweet>("tweets3");
var slangs = db.GetCollection<Slang>("slangs");
var stopwords = db.GetCollection<StopWord>("stopwords");
//string[] ReplaceSlangs(string[] words)
//{
//    for (int i = 0; i < words.Length; i++)
//    {
//        var found = slangs.Find(x => x.acronym == words[i]);
//        if (found.Count() > 0)
//        {
//            words[i] = found.FirstOrDefault().expansion;
//        }
//    }
//    return words;
//}
//string[] RemoveStopWords(string[] words)
//{
//    var wordslist = new List<string>(words);
//    var allsw = stopwords.Find(FilterDefinition<StopWord>.Empty).ToList();
//    foreach (var sw in allsw)
//    {
//        wordslist.Remove(sw.word);
//    }
//    return wordslist.ToArray();
//}
//string RemoveHashtagsUrlMention(string text)
//{
//    text = Regex.Replace(text, "http[^ ,]+", "");
//    text = Regex.Replace(text, "pic.twitter[^ ,]+", "");
//    text = Regex.Replace(text, "#[^ .,]+", "");
//    text = Regex.Replace(text, "@[^ .,]+", "");
//    return text;
//}

//string[] RemoveElongatedNumber(string[] words)
//{
//    var newlist = new List<string>();
//    foreach (var word in words)
//    {
//        if (Regex.IsMatch(word, "^[\\d]+$"))
//        {
//            //nothing
//        }
//        else
//        {
//            newlist.Add(word);
//        }
//    }
//    return newlist.ToArray();
//}
//var test = RemoveHashtagsUrlMention("Yes! And where the hell are these? Jack Ma donated 500,000 testing kits and 1 million masks.  #WhereAreTheMasks #WhereAreTheTestKitshttps://t.co/PTh4xjvuuE");

var words = new Dictionary<string, int>();
var alltweets = tweets.Find(FilterDefinition<Tweet>.Empty).ToList().ToArray();
var tcount = alltweets.Length;
foreach (var tweet in alltweets)
{
    if (tweet.fullText != null && tweet.fullText != "FORBIDDEN" && tweet.fullText != "NOT_FOUND")
    {
        var distinct = tweet.words.Distinct();
        foreach (var word in distinct)
        {
            if (words.ContainsKey(word))
            {
                words[word] += 1;
            }
            else
            {
                words.Add(word, 1);
            }
        }
    }
}
var wcollection = db.GetCollection<SKEMWords>("words");
List<SKEMWords> wordslist = new List<SKEMWords>();
foreach (var w in words)
{
    wordslist.Add(new SKEMWords() { count = w.Value, word = w.Key, SIR = (double)w.Value / (double)tcount });
}
//wcollection.InsertMany(wordslist, new InsertManyOptions { IsOrdered = false });
wordslist.Sort((a, b) => Math.Sign(b.SIR - a.SIR));
var filtered = wordslist.Take(10000).ToArray();
var graph = new BidirectionalGraph<string, Edge<string>>();
//int i = 0;
//double sum = 0;
//foreach (var word in filtered)
//{
//    foreach (var word2 in filtered)
//    {
//        if (word.word != word2.word)
//        {
//            i++;
//            sum += Math.Abs(word.SIR - word2.SIR);
//        }
//    }
//}
//var avg = sum / i;
var k = 0.02;
var labellist = new List<string>();
for (int i = 0; i < filtered.Length; i++)
{
    for (int j = i + 1; j < filtered.Length; j++)
    {
        if (filtered[i].word != filtered[j].word)
        {
            if (Math.Abs(filtered[i].SIR - filtered[j].SIR) >= k)
            {
                if (!graph.ContainsVertex(filtered[i].word))
                {
                    graph.AddVertex(filtered[i].word);
                    labellist.Add(filtered[i].word);
                }
                if (!graph.ContainsVertex(filtered[j].word))
                {
                    graph.AddVertex(filtered[j].word);
                    labellist.Add(filtered[j].word);
                }
                graph.AddEdge(new Edge<string>(filtered[i].word, filtered[j].word));
            }
        }
    }
}
SKEM.Neo4jFileEngine.GenerateCypher("skem.cypher", graph);


