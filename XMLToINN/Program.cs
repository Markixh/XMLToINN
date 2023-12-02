using System.Xml;

namespace XMLToINN
{
    internal class Program
    {
        static List<string> INNUL = new List<string>();
        static List<string> INNFL = new List<string>();

        static void Main(string[] args)
        {
            List<Thread> Threads = new List<Thread>();
            var curentPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var XMLPath = Path.Combine(curentPath, "XML");
            var OutPath = Path.Combine(curentPath, "OUT");
            var XMLFiles = Directory.GetFiles(XMLPath);

            Console.WriteLine($"Найдено {XMLFiles.Length} XML-файлов");

            int c = 0;
            foreach (var file in XMLFiles)
            {
                var thread = new Thread(parserXML);
                Threads.Add(thread);
                thread.Start(file);
                if(c++ == 100)
                {
                    c = 0;
                    Console.SetCursorPosition(0, 1);
                    var pr = (Threads.Count * 100) / XMLFiles.Length;
                    Console.WriteLine($"{Threads.Count}/{XMLFiles.Length} {pr}%");
                }
            }

            while (!Threads.Any(x => x.Join(1000)))
            {
                Console.SetCursorPosition(0, 1);
                var pr = (Threads.Count * 100) / XMLFiles.Length;
                Console.WriteLine($"{Threads.Count}/{XMLFiles.Length} {pr}%");
            }

            Console.WriteLine("Получено:");
            Console.WriteLine($"ИНН ФЛ {INNFL.Count}");
            Console.WriteLine($"ИНН ЮЛ {INNUL.Count}");

            File.AppendAllLinesAsync(Path.Combine(OutPath, "ИННФЛ.txt"), INNFL);
            File.AppendAllLinesAsync(Path.Combine(OutPath, "ИННЮЛ.txt"), INNUL);

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
                                lock (INNFL)
                                {
                                    innFL.Add(INN);
                                }
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
                                lock (INNUL)
                                {
                                    innUL.Add(INN);
                                }
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
