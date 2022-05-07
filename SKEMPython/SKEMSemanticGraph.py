import networkx
import numpy
import matplotlib.pyplot as plt

def get_database():
    # Create a connection using MongoClient. You can import MongoClient or use pymongo.MongoClient
    from pymongo import MongoClient
    client = MongoClient()

    # Create the database for our example (we will use the same database throughout the tutorial
    return client['SKEMDB']



if __name__ == "__main__":    
    g = networkx.Graph()
    # Get the database
    dbname = get_database()
    collection=dbname.get_collection("words")
    words=list(collection.find())
    k=0.0000668061353488438
    i=0
    for w in words:
        i+=1
        print(i)
        word=w["word"]
        for wj in words:
            if wj["word"]!=word:
                if wj["SIR"]>=k and w["SIR"]>=k:
                    g.add_edge(w["word"],wj["word"])

    networkx.draw_spring(g,with_labels=True)
    plt.savefig("graph.png")
    plt.show()  