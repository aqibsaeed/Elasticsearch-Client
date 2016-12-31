using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ESClient
{
    class ES
    {
        #region Variables and Constructor

        const string INDEX = "docs";
        const string URL = @"http://localhost:9200";
        string requestURL = string.Format("{0}/{1}/{2}", URL, INDEX, "_search");

        static ElasticClient client;
        static ES()
        {
            InitClient();
        }

        #endregion

        public void Index(List<Doc> docs)
        {
            try
            {
                foreach (Doc d in docs)
                    client.Index(d);       
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<Answer> Search(Query query)
        {
            int rank = 1;
            List<Answer> answers = new List<Answer>();
            string q = query.Text.Replace("\"", "");

            string jsonPayload = @"{""size"":""100"",""from"":""0"",""query"" : { ""match"": { ""description"": { ""query"" : """ + q + "\" } } } }";

            string response = ExecuteGet(jsonPayload);
            RootObject root = JsonConvert.DeserializeObject<RootObject>(response);
            var hits = root.hits.hits;

            foreach (var hit in hits)
            {
                Answer a = new Answer();
                a.QId = query.QId;
                a.DocId = hit._id;
                a.Rank = rank++;
                a.Sim = hit._score;
                answers.Add(a);
            }

            if (answers.Count < 100)
                Console.WriteLine(query.QId + " " + query.Text + " " + answers.Count);

            return answers;
        }

        #region Private Methods

        private string ExecuteGet(string json)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestURL);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                return streamReader.ReadToEnd();
        }

        private static void InitClient()
        {
            var node = new Uri(URL);
            var settings = new ConnectionSettings(node, INDEX);

            client = new ElasticClient(settings);
        }

        #endregion
    }

    #region Metadata Classes

    public class Shards
    {
        public int total { get; set; }
        public int successful { get; set; }
        public int failed { get; set; }
    }

    public class Source
    {
        public int id { get; set; }
        public string description { get; set; }
    }

    public class Hit
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public double _score { get; set; }
        public Source _source { get; set; }
    }

    public class Hits
    {
        public int total { get; set; }
        public double? max_score { get; set; }
        public List<Hit> hits { get; set; }
    }

    public class RootObject
    {
        public int took { get; set; }
        public bool timed_out { get; set; }
        public Shards _shards { get; set; }
        public Hits hits { get; set; }
    }

    #endregion
}
