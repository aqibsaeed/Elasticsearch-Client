using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ESClient
{
    class FileHelper
    {
        //Replace these path values for your machine
        const string RESULTFILEPATH = @"C:\...";
        const string OLDRESULTFILEPATH = @"C:\...";
        const string COMPARSIONRESULTFILEPATH = @"C:\...";
        const string QUERYFILEPATH = @"C:\...";
        const string COLLECTIONFILEPATH = @"C:\...";

        const string GROUNDTRUTHFILEPATH = @"C:\...";
        const string TESTRUNFILEPATH = @"C:\...";
        const string EVALUATIONFILEPATH = @"C:\...";

        public List<Doc> ReadDocFile()
        {
            string[] text = File.ReadAllLines(COLLECTIONFILEPATH);
            List<Doc> docs = new List<Doc>();

            for (int i = 1; i < text.Length; i++)
            {
                string[] s = text[i].Split('\t');
                Doc p = new Doc();
                p.Id = Convert.ToInt32(s[0]);
                p.Description = s[1].ToLower();
                docs.Add(p);
            }

            return docs;
        }

        public List<Query> ReadQueryFile()
        {
            string[] text = File.ReadAllLines(QUERYFILEPATH);
            List<Query> queries = new List<Query>();


            for (int i = 1; i < text.Length; i++)
            {
                string[] s = text[i].Split('\t');
                Query q = new Query();
                q.QId = Convert.ToInt32(s[0]);
                q.Text = s[1].ToLower();

                queries.Add(q);
            }

            return queries;
        }

        public List<GroundTruth> ReadGroundTruthFile()
        {
            string[] text = File.ReadAllLines(GROUNDTRUTHFILEPATH);
            List<GroundTruth> groundTruths = new List<GroundTruth>();

            for (int i = 0; i < text.Length; i++)
            {
                string[] s = text[i].Split(' ');
                GroundTruth groundTruth = new GroundTruth();
                groundTruth.Qid = Convert.ToInt32(s[0]);
                groundTruth.Docid = s[2];
                groundTruth.Rel = Convert.ToInt32(s[3]);
                groundTruths.Add(groundTruth);
            }


            return groundTruths;
        }

        public List<Answer> ReadBaseLineFile()
        {
            string[] text = File.ReadAllLines(TESTRUNFILEPATH);
            List<Answer> answers = new List<Answer>();

            for (int i = 0; i < text.Length; i++)
            {
                if(string.IsNullOrEmpty(text[i]))
                    continue;

                string[] s = text[i].Split('\t');
                Answer ans = new Answer();
                ans.QId = Convert.ToInt32(s[0]);
                ans.DocId = s[2];
                ans.Rank = Convert.ToInt32(s[3]);
                ans.Sim = Convert.ToDouble(s[4]);
                answers.Add(ans);
            }

            return answers;
        }

        public void WriteResultFile(List<Answer> answers)
        {
            List<string> text = new List<string>();
            text.Add("Qid\tIter\tDocid\tRank\tSim\tRunid");
            foreach (Answer a in answers)
                text.Add(string.Format("{0}\t{1}\t{2}\t{3}",
                            a.QId,a.DocId,a.Rank,a.Sim));

            File.WriteAllLines(RESULTFILEPATH, text.ToArray());
        }

        //Calculate P5, P30, P1000, MAP and RecpRank with AVGS
        public void Evaluate()
        {
            List<GroundTruth> groundTruth = ReadGroundTruthFile();
            List<Answer> baseLine = ReadBaseLineFile();
            List<EvaluationResult> answers = new List<EvaluationResult>();
            groundTruth.DistinctBy(i => i.Qid).ToList().ForEach(q=>answers.Add(new EvaluationResult(){ Qid = q.Qid }));
            double MAPAvg = 0.0, P1Avg = 0.0 , P5Avg = 0.0, P30Avg = 0.0, P1000Avg = 0.0, RecpRankAvg = 0.0, 
                S1Avg = 0.0 , S5Avg = 0.0, S30Avg = 0.0, S1000Avg = 0.0;
            List<string> output = new List<string>();

            foreach (var q in answers)
            {
                bool isRecpSet = false;
                var bl = baseLine.Where(b => b.QId == q.Qid).ToList();
                int relevantDocs = groundTruth.Where(i => i.Qid == q.Qid && i.Rel == 1).Count();

                //Calculate P1
                GroundTruth t = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == bl.First().DocId).ToList().FirstOrDefault();

                if (t != null)
                    if (t.Rel == 1)
                        q.P1++;

                //Calculate P5
                for (int i = 0; i < 5; i++)
                {
                    if (i == bl.Count)
                        break;

                    GroundTruth truth = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == bl[i].DocId).ToList().FirstOrDefault();

                    if (truth != null)
                        if (truth.Rel == 1) 
                            q.P5++;
                }

                //Calculate P30
                for (int i = 0; i < 30; i++)
                {
                    if (i == bl.Count)
                        break;

                    GroundTruth truth = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == bl[i].DocId).ToList().FirstOrDefault();

                    if (truth != null)
                        if (truth.Rel == 1)
                            q.P30++;
                }

                //Calculate P1000
                for (int i = 0; i < 1000; i++)
                {
                    if (i == bl.Count)
                        break;

                    GroundTruth truth = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == bl[i].DocId).ToList().FirstOrDefault();

                    if (truth != null)
                        if (truth.Rel == 1)
                            q.P1000++;
                }

                //Calculate Recp Rank
                foreach (var item in bl)
                {
                    GroundTruth truth = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == item.DocId).ToList().FirstOrDefault();

                    if (truth != null)
                    {
                        if (truth.Rel == 1)
                        {
                            if (!isRecpSet)
                                q.RecpRank++;

                            break;
                        }
                        else if (!isRecpSet)
                            q.RecpRank++;
                    }
                }

                //Calculate MAP
                double prec = 0.0;
                int m = 1;
                for (int i = 0; i < bl.Count; i++)
			    {
                    double k = i + 1;
                    GroundTruth truth = groundTruth.Where(qr => qr.Qid == q.Qid && qr.Docid == bl[i].DocId).ToList().FirstOrDefault();

                    if (truth != null)
                    {
                        if (truth.Rel == 1) {
                            prec = prec + (m / k);
                            m++;
                        }
                    }
			    }

                if (q.P1 > 0)
                    q.S1 = 1;
                if (q.P5 > 0)
                    q.S5 = 1;
                if (q.P30 > 0)
                    q.S30 = 1;
                if (q.P1000 > 0)
                    q.S1000 = 1;

                q.MAP = Math.Round((prec / relevantDocs),4);
                q.P5 = Math.Round((q.P5 / 5),4);
                q.P30 = Math.Round((q.P30 / 30),4);
                q.P1000 = Math.Round((q.P1000 / 1000),4);
                q.RecpRank = Math.Round((1 / q.RecpRank),4);
            }

            //Calculate AVG and add answers to output
            foreach(EvaluationResult ans in answers)
            {
                output.Add(string.Format("{0} {1} {2}","map",ans.Qid,ans.MAP));
                output.Add(string.Format("{0} {1} {2}", "P1", ans.Qid, ans.P1));
                output.Add(string.Format("{0} {1} {2}", "P5", ans.Qid, ans.P5));
                output.Add(string.Format("{0} {1} {2}", "P30", ans.Qid, ans.P30));
                output.Add(string.Format("{0} {1} {2}", "P1000", ans.Qid, ans.P1000));
                output.Add(string.Format("{0} {1} {2}", "recip_rank", ans.Qid, ans.RecpRank));
                output.Add(string.Format("{0} {1} {2}", "S1", ans.Qid, ans.S1));
                output.Add(string.Format("{0} {1} {2}", "S5", ans.Qid, ans.S5));
                output.Add(string.Format("{0} {1} {2}", "S30", ans.Qid, ans.S30));
                output.Add(string.Format("{0} {1} {2}", "S1000", ans.Qid, ans.S1000));
                

                MAPAvg += ans.MAP;
                P1Avg += ans.P1;
                P5Avg += ans.P5;
                P30Avg += ans.P30;
                P1000Avg += ans.P1000;
                RecpRankAvg += ans.RecpRank;
                S1Avg += ans.S1;
                S5Avg += ans.S5;
                S30Avg += ans.S30;
                S1000Avg += ans.S1000;
            }

            MAPAvg = Math.Round((MAPAvg / answers.Count),4);
            P1Avg = Math.Round((P1Avg / answers.Count), 4);
            P5Avg = Math.Round((P5Avg / answers.Count),4);
            P30Avg = Math.Round((P30Avg / answers.Count),4);
            P1000Avg = Math.Round((P1000Avg / answers.Count),4);
            RecpRankAvg = Math.Round((RecpRankAvg / answers.Count),4);
            S1Avg = Math.Round((S1Avg / answers.Count), 4);
            S5Avg = Math.Round((S5Avg / answers.Count),4);
            S30Avg = Math.Round((S30Avg / answers.Count), 4);
            S1000Avg = Math.Round((S1000Avg / answers.Count), 4);


            output.Add(string.Format("{0} {1} {2}", "map", "all", MAPAvg));
            output.Add(string.Format("{0} {1} {2}", "P1", "all", P1Avg));
            output.Add(string.Format("{0} {1} {2}", "P5", "all", P5Avg));
            output.Add(string.Format("{0} {1} {2}", "P30", "all", P30Avg));
            output.Add(string.Format("{0} {1} {2}", "P1000", "all", P1000Avg));
            output.Add(string.Format("{0} {1} {2}", "recp_rank", "all", RecpRankAvg));
            output.Add(string.Format("{0} {1} {2}", "S1", "all", S1Avg));
            output.Add(string.Format("{0} {1} {2}", "S5", "all", S5Avg));
            output.Add(string.Format("{0} {1} {2}", "S30", "all", S30Avg));
            output.Add(string.Format("{0} {1} {2}", "S1000", "all", S1000Avg));

            File.WriteAllLines(EVALUATIONFILEPATH, output.ToArray());
        }
    }

    #region Helpers

    public static class FunctionHelper
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }

    #endregion  
}
