// See https://aka.ms/new-console-template for more information
using MongoDB.Driver;
using SKEMCollections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

var client = new MongoClient();
var db = client.GetDatabase("SKEMDB");
var tweets = db.GetCollection<Tweet>("tweets3");
var slangs = db.GetCollection<Slang>("slangs");
var stopwords = db.GetCollection<StopWord>("stopwords");
string[] ReplaceSlangs(string[] words)
{
    for (int i = 0; i < words.Length; i++)
    {
        var found = slangs.Find(x => x.acronym == words[i]);
        if (found.Count() > 0)
        {
            words[i] = found.FirstOrDefault().expansion;
        }
    }
    return words;
}
string[] RemoveStopWords(string[] words)
{
    var wordslist = new List<string>(words);
    var allsw = stopwords.Find(FilterDefinition<StopWord>.Empty).ToList();
    foreach (var sw in allsw)
    {
        wordslist.Remove(sw.word);
    }
    return wordslist.ToArray();
}
string RemoveHashtagsUrlMention(string text)
{
    text = Regex.Replace(text, "http[^ ,]+", "");
    text = Regex.Replace(text, "pic.twitter[^ ,]+", "");
    text = Regex.Replace(text, "#[^ .,]+", "");
    text = Regex.Replace(text, "@[^ .,]+", "");
    return text;
}

string[] RemoveElongatedNumber(string[] words)
{
    var newlist = new List<string>();
    foreach (var word in words)
    {
        if (Regex.IsMatch(word, "^[\\d]+$"))
        {
            //nothing
        }
        else
        {
            newlist.Add(word);
        }
    }
    return newlist.ToArray();
}
var test = RemoveHashtagsUrlMention("Yes! And where the hell are these? Jack Ma donated 500,000 testing kits and 1 million masks.  #WhereAreTheMasks #WhereAreTheTestKitshttps://t.co/PTh4xjvuuE");

var alltweets = tweets.Find(FilterDefinition<Tweet>.Empty).ToList().ToArray();
foreach (var tweet in alltweets)
{
    if (tweet.fullText != null && tweet.fullText != "FORBIDDEN" && tweet.fullText != "NOT_FOUND")
    {
        var text = RemoveHashtagsUrlMention(tweet.fullText);
        var splitted = text.ToLower().Split(new char[] { ' ', '.', ',','!','?',':',';','\'','\"' }, StringSplitOptions.RemoveEmptyEntries);
        var words = RemoveStopWords(splitted);
        words = RemoveElongatedNumber(words);
        words = ReplaceSlangs(words);
        var preprocessed = string.Join(' ', words);
    }
}