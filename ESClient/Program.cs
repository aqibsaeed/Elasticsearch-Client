using System;
using System.Collections.Generic;

namespace ESClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                FileHelper helper = new FileHelper();
                ES client = new ES();

                Console.WriteLine("Enter 1 for Indexing, 2 for Search, 3 for Evaluation:");
                int opt = Convert.ToInt32(Console.ReadLine());
                switch (opt)
                {
                    case 1:
                        Console.WriteLine("Indexing Start");
                        List<Doc> products = helper.ReadDocFile();
                        client.Index(products);
                        Console.WriteLine("Indexing Complete");
                        break;
                    case 2:
                        Console.WriteLine("Search Start");
                        List<Answer> answers = new List<Answer>();
                        List<Query> queries = helper.ReadQueryFile();
                        foreach (Query q in queries)
                            answers.AddRange(client.Search(q));

                        helper.WriteResultFile(answers);
                        Console.WriteLine("Search End");
                        break;
                    case 3:
                        Console.WriteLine("Evaluation Start");
                        helper.Evaluate();
                        Console.WriteLine("Evaluation Complete");
                        break;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
