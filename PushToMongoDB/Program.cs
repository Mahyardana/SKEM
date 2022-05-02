// See https://aka.ms/new-console-template for more information
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using SKEMCollections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

long tweetsfetched = 0;
Dictionary<string, int[]> checkpoints = new Dictionary<string, int[]>();
void FetchThread(IMongoCollection<Tweet> tweets, string[] lines, Tweet[] indbtweets, int threadnum, string filename)
{
    var webclient = new WebClient();
    //var notfounds = new List<string>();
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var splitted = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (i >= checkpoints[filename][threadnum])
        {
            if (indbtweets.FirstOrDefault(x => x.tweetID == splitted[0]) == null)
            {
                try
                {
                    var tweet = webclient.DownloadString("https://publish.twitter.com/oembed?dnt=true&omit_script=true&url=https://mobile.twitter.com/i/status/" + splitted[0]);
                    var json = JObject.Parse(tweet);
                    var html = json["html"].ToString();
                    var fulltext = Encoding.UTF8.GetString(Encoding.ASCII.GetBytes(html));
                    fulltext = WebUtility.HtmlDecode(fulltext);
                    var match = Regex.Match(fulltext, "<p[^>]*>([\\s\\S]+)<\\/p>");
                    var insidetext = Regex.Replace(match.Groups[1].Value, "<[^>]*>", "");
                    //Console.WriteLine(insidetext);
                    tweetsfetched++;
                    var newtweet = new Tweet() { authorName = json["author_name"].ToString(), authorUrl = json["author_url"].ToString(), fullText = insidetext, tweetID = splitted[0], sentimentScore = Convert.ToDouble(splitted[1]) };
                    tweets.InsertOne(newtweet);
                }
                catch (WebException ex)
                {
                    var response = ex.Response as System.Net.HttpWebResponse;
                    if (response == null)
                        continue;
                    else if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        //Thread.Sleep(60000);
                        var newtweet = new Tweet() { authorName = null, authorUrl = null, fullText = "FORBIDDEN", tweetID = splitted[0], sentimentScore = 0 };
                        tweets.InsertOne(newtweet);
                        //continue;
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        //notfounds.Add(line);
                        var newtweet = new Tweet() { authorName = null, authorUrl = null, fullText = "NOT_FOUND", tweetID = splitted[0], sentimentScore = 0 };
                        tweets.InsertOne(newtweet);
                        //Thread.Sleep(10000);
                    }
                }
            }
            checkpoints[filename][threadnum] = i;
        }
    }
}

void UpdateCheckpoints()
{
    var strings = new string[checkpoints.Count];
    for (int i = 0; i < strings.Length; i++)
    {
        var curr = checkpoints.ElementAt(i);
        strings[i] = curr.Key + "," + String.Join(',', Array.ConvertAll<int, string>(curr.Value, Convert.ToString));
    }
    File.WriteAllLines("checkpoints.txt", strings);
}

void main()
{
    #region Checkpoints
    if (File.Exists("checkpoints.txt"))
    {
        var checkpointlines = File.ReadAllLines("checkpoints.txt");
        foreach (var checkpoint in checkpointlines)
        {
            var splitted = checkpoint.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            checkpoints.Add(splitted[0], Array.ConvertAll<string, int>(splitted.Skip(1).ToArray(), s => int.Parse(s)));
        }
    }
    #endregion

    MongoClient mongoClient = new MongoClient();
    var db = mongoClient.GetDatabase("SKEMDB");

    #region Slangs
    var slangs = db.GetCollection<Slang>("slangs");
    slangs.Indexes.CreateOne(new CreateIndexModel<Slang>(new IndexKeysDefinitionBuilder<Slang>().Ascending(x => x.acronym), new CreateIndexOptions() { Unique = true }));
    var lines = File.ReadAllLines("slang.csv");
    var slangstopush = new List<Slang>();
    foreach (var line in lines)
    {
        var splitted = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (splitted.Length == 3)
        {
            var newslang = new Slang();
            newslang.acronym = splitted[1];
            newslang.expansion = splitted[2];
            slangstopush.Add(newslang);
        }
    }
    try
    {
        slangs.InsertMany(slangstopush, new InsertManyOptions() { IsOrdered = false });
    }
    catch (Exception ex)
    {

    }
    #endregion

    #region StopWords
    var stopwords = db.GetCollection<StopWord>("stopwords");
    stopwords.Indexes.CreateOne(new CreateIndexModel<StopWord>(new IndexKeysDefinitionBuilder<StopWord>().Ascending(x => x.word), new CreateIndexOptions() { Unique = true }));
    lines = File.ReadAllLines("stopwords.txt");
    var stopwordstopush = new List<StopWord>();
    foreach (var line in lines)
    {
        var newstopword = new StopWord();
        newstopword.word = line;
        stopwordstopush.Add(newstopword);
    }
    try
    {
        stopwords.InsertMany(stopwordstopush, new InsertManyOptions() { IsOrdered = false });
    }
    catch (Exception ex)
    {

    }
    #endregion

    #region FetchTweets
    foreach (var csv in Directory.GetFiles("CSV", "*.csv"))
    {
        lines = File.ReadAllLines(csv);
        var filteredlines = new List<string>();

        var tweets = db.GetCollection<Tweet>("tweets3");
        try
        {
            tweets.Indexes.CreateOne(new CreateIndexModel<Tweet>(new IndexKeysDefinitionBuilder<Tweet>().Ascending(x => x.tweetID), new CreateIndexOptions() { Unique = true }));
        }
        catch
        {

        }

        var alltweets = tweets.Find(FilterDefinition<Tweet>.Empty).ToList().ToArray();
        var threadnum = 50;
        var totake = lines.Length / threadnum;
        var threads = new List<Thread>();
        if (!checkpoints.ContainsKey(csv))
        {
            checkpoints.Add(csv, new int[threadnum]);
        }
        for (int k = 0; k < threadnum; k++)
        {
            var part = totake;
            if (lines.Length < totake)
                part = lines.Length;
            int tid = k;
            var thread = new Thread(() => { FetchThread(tweets, lines.Take(part).ToArray(), alltweets, tid, csv); });
            lines = lines.Skip(part).ToArray();
            threads.Add(thread);
            thread.Start();
        }
        while (threads.Any(x => x.IsAlive))
        {
            Console.Clear();
            Console.WriteLine(csv);
            Console.WriteLine(tweetsfetched);
            Console.WriteLine(string.Join(',', Array.ConvertAll<int, string>(checkpoints[csv], Convert.ToString)));
            UpdateCheckpoints();
            Thread.Sleep(1000);
        }
        UpdateCheckpoints();
    }
    #endregion
}
main();