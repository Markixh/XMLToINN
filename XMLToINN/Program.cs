using System.Diagnostics;
using System.Xml;

namespace XMLToINN
{
    internal class Program
    {
        static List<string> INNUL = new List<string>();
        static List<string> INNFL = new List<string>();

        static async Task Main(string[] args)
        {
            List<Thread> Threads = new List<Thread>();
            var curentPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var XMLPath = Path.Combine(curentPath, "XML");
            var OutPath = Path.Combine(curentPath, "OUT");
            var XMLFiles = Directory.GetFiles(XMLPath);

            Console.WriteLine($"Найдено {XMLFiles.Length} XML-файлов");

            foreach (var file in XMLFiles)
            {
                ThreadPool.QueueUserWorkItem(parserXML, file);

                var procent = (int)((ThreadPool.CompletedWorkItemCount * 100) / XMLFiles.Length);
                Console.SetCursorPosition(0, 1);
                Console.WriteLine($"{ThreadPool.CompletedWorkItemCount} из {XMLFiles.Length} {procent}%");                
            }

            while (ThreadPool.PendingWorkItemCount != 0)
            {
                var procent = (int)((ThreadPool.CompletedWorkItemCount * 100) / XMLFiles.Length);
                Console.SetCursorPosition(0, 1);
                Console.WriteLine($"{ThreadPool.CompletedWorkItemCount} из {XMLFiles.Length} {procent}%");
            }

            Console.SetCursorPosition(0, 1);
            Console.WriteLine($"{XMLFiles.Length} из {XMLFiles.Length} 100%");

            Console.WriteLine("Получено:");

            lock (INNFL)
            {
                Console.WriteLine($"ИНН ФЛ {INNFL.Count}");
                File.AppendAllLinesAsync(Path.Combine(OutPath, "ИННФЛ.txt"), INNFL).Wait();
            }
            lock (INNUL)
            {
                Console.WriteLine($"ИНН ЮЛ {INNUL.Count}");
                File.AppendAllLinesAsync(Path.Combine(OutPath, "ИННЮЛ.txt"), INNUL).Wait();
            }
            Console.WriteLine("Списки сохранены в файлы.");

            Console.ReadKey();
        }

        private static void parserXML(object? path)
        {
            XmlReader xmlReader = XmlReader.Create((string)path);
            List<string> innUL = new List<string>();
            List<string> innFL = new List<string>();

            while (xmlReader.Read())
            {
                string INN = string.Empty;
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "ИПВклМСП"))
                {
                    if (xmlReader.HasAttributes)
                        INN = xmlReader.GetAttribute("ИННФЛ");
                    xmlReader.Read();
                    xmlReader.Read();
                    xmlReader.Read();
                    if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "СведМН"))
                    {
                        if (xmlReader.HasAttributes)
                            if (xmlReader.GetAttribute("КодРегион") == "46")
                            {
                                innFL.Add(INN);
                            }
                    }
                }
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "ОргВклМСП"))
                {
                    if (xmlReader.HasAttributes)
                        INN = xmlReader.GetAttribute("ИННЮЛ");
                    xmlReader.Read();
                    if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "СведМН"))
                    {
                        if (xmlReader.HasAttributes)
                            if (xmlReader.GetAttribute("КодРегион") == "46")
                            {
                                innUL.Add(INN);
                            }
                    }
                }
            }

            if (innFL.Count > 0)
            {
                lock (INNFL)
                {
                    INNFL.AddRange(innFL);
                }
            }

            if (innUL.Count > 0)
            {
                lock (INNUL)
                {
                    INNUL.AddRange(innUL);
                }
            }
        }
    }
}
