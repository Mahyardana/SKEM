from pymongo import MongoClient
import pymongo
import re
import os
import nltk.corpus
from nltk.stem import PorterStemmer 
from nltk.tokenize import word_tokenize
from nltk.corpus import stopwords
stop = stopwords.words('english')
nltk.download('stopwords')
nltk.download('wordnet')
nltk.download('omw-1.4')
from nltk.stem import WordNetLemmatizer 

def get_database():
    # Create a connection using MongoClient. You can import MongoClient or use pymongo.MongoClient
    from pymongo import MongoClient
    client = MongoClient()

    # Create the database for our example (we will use the same database throughout the tutorial
    return client['SKEMDB']
    
def clean_text(text):
    cleaned = text.lower()
    cleaned = re.sub(r"(@[A-Za-z0-9]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)|^rt|http.+?", "", cleaned)  
    # remove numbers
    cleaned =  re.sub(r"\d+", "", cleaned)
    cleaned=' '.join([word for word in cleaned.split() if word not in (stop)])
    return cleaned

def stemmer(text):
    ps = PorterStemmer()
    words=text.split()
    stemwords=[]
    for word in words:
        stemwords.append(ps.stem(word))
    return ' '.join(stemwords)

def word_lemmatizer(text):
    lemtext=[]
    for t in text:
        lemtext.append(WordNetLemmatizer().lemmatize(t))
    return lemtext

def slangsreplace(words,slangs):
    replaced=[]
    for w in words:
        found= [x for x in slangs if x["acronym"]==w]
        if found.__len__() > 0:
            replaced.append( found[0]["expansion"])
        else:
            replaced.append(w)
    return ' '.join(replaced)


# This is added so that many files can reuse the function get_database()
if __name__ == "__main__":    
    
    # Get the database
    dbname = get_database()
    collection=dbname.get_collection("tweets3")
    slangscollection=dbname.get_collection("slangs")
    slangs=list(slangscollection.find())
    tweets=list(collection.find())
    #remove hashtags
    i=0
    for tweet in tweets:
        if ((tweet["fullText"] is not None) and (tweet["fullText"] != "FORBIDDEN") and (tweet["fullText"] != "NOT_FOUND")):
            text=tweet["fullText"].lower()
            text=re.sub(r"http[^ ,]+","",text)
            text=re.sub(r"pic.twitter[^ ,]+","",text)
            text=re.sub(r"\#[^ ,.]+","",text)
            text=re.sub(r"\@[^ ,.]+","",text)
            text=clean_text(text)
            text=slangsreplace(text.split(),slangs)
            text=stemmer(text)
            text=word_lemmatizer(text.split())
            i+=1
            tweet["words"]=text
            collection.update_one(
                {"_id":tweet["_id"]},
                {"$set":{"words":tweet["words"]}}
                )