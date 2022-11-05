using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

public class Program
{
    static void Main(string[] args)
    {
        string currPath = System.IO.Directory.GetCurrentDirectory();

        string[] commands = new string[] { "clean", "count", "singleword" };

        string helpStr = @"
         -e [command] (" + String.Join(", ", commands) + @")
              example: clean -column 4 -in commenti.csv -out data.csv -skip skip.csv
                       count -column 4 -in commenti.csv -out data.csv -words parole.csv
                       singleword -column 4 -in commenti.csv -out singlewords.csv -skip skip.csv

         -h: show this
         -column [number]: colonna dei commenti
         -in: nome del file in entrata
         -out: nome del file in uscita
         -words: parole da cercare
         -skip: parole da saltare
        ";
        string command, inFile, outFile, wordsFile, skipFile;
        int column = 0;

        if (args[0] == "-h")
        {
            Console.WriteLine(helpStr);
            return;
        }

        command = GetArgument(args, "-e");

        if (command == null || !commands.Contains(command))
            Console.WriteLine("Inserire un command, e.g. -h");

        inFile = GetArgument(args, "-in");
        if (inFile == null)
        {
            Console.WriteLine("Inserire nome file in input, usare -in");
            return;
        }

        if (!File.Exists($"{currPath}\\{inFile}"))
        {
            Console.WriteLine("File in input inesistente");
            return;
        }

        outFile = GetArgument(args, "-out");
        if (outFile == null)
        {
            Console.WriteLine("Inserire nome file in output, usare -out");
            return;
        }

        if (!int.TryParse(GetArgument(args, "-column"), out column))
        {
            Console.WriteLine("Colonna sconosciuta");
            return;
        }

        if (!Directory.Exists($"{currPath}\\export"))
            Directory.CreateDirectory($"{currPath}\\export");

        if (command == "clean")
        {
            skipFile = GetArgument(args, "-skip");

            if (args.FirstOrDefault(a => a == "-skip") != null && !File.Exists($"{currPath}\\{skipFile}"))
            {
                Console.WriteLine("File parole inesistente");
                return;
            }

            Clean(inFile, outFile, $"{currPath}\\{skipFile}", column);
        }
        else if (command == "count")
        {
            wordsFile = GetArgument(args, "-words");

            if (args.FirstOrDefault(a => a == "-words") != null && !File.Exists($"{currPath}\\{wordsFile}"))
            {
                Console.WriteLine("File parole inesistente");
                return;
            }

            Count(inFile, outFile, $"{currPath}\\{wordsFile}", column);
        }
        else if (command == "singleword")
        {
            skipFile = GetArgument(args, "-skip");

            if (args.FirstOrDefault(a => a == "-skip") != null && !File.Exists($"{currPath}\\{skipFile}"))
            {
                Console.WriteLine("File parole inesistente");
                return;
            }

            SingleWord(inFile, outFile, $"{currPath}\\{skipFile}", column);
        }
        else
        {
            Console.WriteLine("Command sconosciuto");
            return;
        }
        Console.WriteLine("Sono fatto!");
    }

    static string GetArgument(string[] args, string option)
    {
        string res = args.FirstOrDefault(a => a.StartsWith($"{option}"));
        if (res == null) return null;
        return args.ToArray()[args.ToList().IndexOf(res) + 1];
    }

    public static List<string> GetWordsToSkip(string skipFilePath)
    {
        if (!File.Exists(skipFilePath))
            return new List<string>();

        List<string> res = new List<string>();
        using (TextFieldParser parser = new TextFieldParser(skipFilePath))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    res.Add(field.ToLower());
                }
            }
        }

        return res;
    }

    public static List<string> GetWords(string wordsFilePath)
    {
        List<string> res = new List<string>();

        if (!File.Exists(wordsFilePath))
            return res;

        using (TextFieldParser parser = new TextFieldParser(wordsFilePath))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    res.Add(field.ToLower());
                }
            }
        }

        return res;
    }

    public static void Clean(string inFilePath, string outFileName, string skipFilePath, int commentColumn)
    {

        StringBuilder csv = new StringBuilder();
        List<string> wordsToSkip = GetWordsToSkip(skipFilePath);
        List<string> nonLatinWords = new List<string>();
        List<string> words = new List<string>();
        decimal tryParse;

        #region pulisco il csv
        using (TextFieldParser parser = new TextFieldParser(inFilePath))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                string field;
                for (int i = 0; i < fields.Count(); i++)
                {
                    field = fields[i];
                    if ((i + 1) == commentColumn)
                    {
                        words = GetCommentSplitted(fields[i]).Where(w => !wordsToSkip.Contains(w)).ToList();
                        field = String.Join(" ", words);
                    }
                    csv.Append($"{field},");
                }
                csv.Append("\n");
            }
        }
        #endregion

        string currPath = System.IO.Directory.GetCurrentDirectory();

        File.WriteAllText($"{currPath}\\export\\{outFileName}", String.Join(",", csv.ToString()));
    }

    public static void Count(string inFilePath, string outFileName, string wordsFilePath, int commentColumn)
    {
        List<string> wordsToFind = GetWords(wordsFilePath);

        var csv = new StringBuilder();

        foreach (string word in wordsToFind)
        {
            using (TextFieldParser parser = new TextFieldParser(inFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    StringBuilder row = new StringBuilder();
                    row.Append($"{word},");
                    int count = 0;
                    for (var i = 0; i < fields.Count(); i++)
                    {
                        if (i < (commentColumn - 1) || i > (commentColumn - 1))
                            row.Append($"{fields[i]},");
                        else if (i == (commentColumn - 1))
                        {
                            count = GetCommentSplitted(fields[i]).Where(p => p == word).Count();
                            row.Append($"{count},");
                        }
                    }
                    if(count > 0)
                        csv.AppendLine(row.ToString());
                }
            }
        }

        string currPath = System.IO.Directory.GetCurrentDirectory();

        File.WriteAllText($"{currPath}\\export\\{outFileName}", String.Join(",", csv.ToString()));
    }

    public static void SingleWord(string inFilePath, string outFileName, string skipFilePath, int commentColumn)
    {
        List<string> wordsToSkip = GetWords(skipFilePath);
        List<string> wordToCount = new List<string>();

        var csv = new StringBuilder();

        using (TextFieldParser parser = new TextFieldParser(inFilePath))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                wordToCount.AddRange(GetCommentSplitted(fields[commentColumn - 1]));
            }
        }

        List<string> nonLatinWords = new List<string>();
        Dictionary<string, int> wordsValue = new Dictionary<string, int>();

        decimal tryParse;
        foreach (var word in wordToCount.Distinct().Where(w => w != null && w.Trim() != "" && !wordsToSkip.Contains(w)).OrderBy(w => w).ToList())
        {
            if (Regex.IsMatch(word, "[^a-zA-Z]") && !word.StartsWith('#') && !decimal.TryParse(word, out tryParse))
            {
                nonLatinWords.Add(word);
            }
            wordsValue.Add(word, wordToCount.Where(w => w == word).ToList().Count());
        }

        foreach (var wv in wordsValue)
        {
            var newLine = string.Format("{0},{1}", wv.Key, wv.Value);
            csv.AppendLine(newLine);
        }

        string currPath = System.IO.Directory.GetCurrentDirectory();

        File.WriteAllText($"{currPath}\\export\\{outFileName}", csv.ToString());
        File.WriteAllText($"{currPath}\\export\\non_latin_words.csv", String.Join(",\n", nonLatinWords));
    }

    public static string CleanWord(string word)
    {
        return word
            .Replace("\u00A0", null)
            .Replace(":", null)
            .Replace(".", null)
            .Replace("-", null)
            .Replace(";", null)
            .Replace(",", null)
            .Replace("'", null)
            .Replace(@"""", null)
            .Replace("(", null)
            .Replace(")", null)
            .Replace("_", null)
            .Replace("«", null)
            .Replace("»", null)
            .Replace("\r", null)
            .Replace("\n", null)
            .Replace("👉", null)
            .Replace("”", null)
            .Replace("“", null)
            .Replace("{", null)
            .Replace("}", null)
            .Replace("[", null)
            .Replace("]", null)
            .Replace("&", null)
            .Replace("„", null)
            .Replace("▶", null)
            .Replace("►", null)
            .Replace("⚡", null)
            .Replace("|", null)
            .Replace("+", null)
            .Replace("*", null)
            .Replace("–", null)
            .Replace("!", null)
            .Replace("?", null)
            .Replace("=", null)
            .Replace("—", null)
            .Replace("—", null)
            .Replace("’", "'")
            .Replace("...", null)
            .Replace("š", "s")
            .Replace("š", "s")
            .Replace("₂", "2")
            .Replace("…", null)
            .Replace("®", null);
    }

    public static List<string> GetCommentSplitted(string comment)
    {
        return comment
                    .Split(" ")
                    .Where(p =>
                        !p.StartsWith("http")
                        && !p.Contains("http")
                        && !p.Contains("@")
                        && p != null
                        && p.Trim() != ""
                        && !p.StartsWith("www")
                        )
                    .Select(p =>
                        CleanWord(p.ToLower())
                    ).ToList();
    }
}