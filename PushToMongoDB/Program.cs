// See https://aka.ms/new-console-template for more information
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using SKEMCollections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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
lines = File.ReadAllLines("corona_tweets_01.csv");
var tweets = db.GetCollection<Tweet>("tweets");
var webclient = new WebClient();
foreach (var line in lines)
{
    var splitted = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    if (tweets.Find(x => x.tweetID == splitted[0]).Count() == 0)
    {
        try
        {
            var tweet = webclient.DownloadString("https://publish.twitter.com/oembed?dnt=true&omit_script=true&url=https://mobile.twitter.com/i/status/" + splitted[0]);
            var json = JObject.Parse(tweet);
            var html = json["html"].ToString();
            var fulltext = Encoding.UTF8.GetString(Encoding.ASCII.GetBytes(html));
            fulltext = WebUtility.HtmlDecode(fulltext);
            var match = Regex.Match(fulltext, "<p[^>]*>([\\s\\S]+)<\\/p>");
            var insidetext = Regex.Replace(match.Groups[1].Value, "<[^>]*>","");
            Console.WriteLine(insidetext);
            var newtweet = new Tweet() { authorName = json["author_name"].ToString(), authorUrl = json["author_url"].ToString(), fullText = insidetext, tweetID = splitted[0], sentimentScore = Convert.ToDouble(splitted[1]) };
            tweets.InsertOne(newtweet);
        }
        catch (WebException ex)
        {
            if (ex.Status == (WebExceptionStatus)403)
                throw new Exception();
        }
        Thread.Sleep(100);
    }
}
#endregion