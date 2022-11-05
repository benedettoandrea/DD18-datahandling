using Microsoft.VisualBasic.FileIO;
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
        string pathParole = $"{currPath}\\{args[0]}";
        string pathData = $"{currPath}\\{args[1]}";
        string pathCommenti = $"{currPath}\\{args[2]}";
        string pathParoleDaConfrontare = $"{currPath}\\{args[3]}";

        if (!Directory.Exists($"{currPath}\\export"))
            Directory.CreateDirectory($"{currPath}\\export");

        DoScript1(pathParole, pathData);
        DoScript2(pathCommenti, pathParoleDaConfrontare);

        Console.WriteLine("Sono fatto!");
    }

    public static void DoScript1(string pathParole, string pathData)
    {
        Dictionary<string, int> numeroParole = new Dictionary<string, int>();

        // per ogni riga spezzo in parole

        var parole = new List<string>();

        List<string> listaDaSkippare = new List<string>();

        using (TextFieldParser parser = new TextFieldParser(pathParole))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    listaDaSkippare.Add(field.ToLower());
                }
            }
        }

        using (TextFieldParser parser = new TextFieldParser(pathData))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    parole.AddRange(GetCommentSplitted(field));
                }
            }
        }

        List<string> paroleRompiScatole = new List<string>();
        decimal tryParse;
        foreach (var parola in parole.Distinct().Where(p => p != null && p.Trim() != "" && !listaDaSkippare.Contains(p)))
        {
            if (Regex.IsMatch(parola, "[^a-zA-Z]") && !parola.StartsWith('#') && !decimal.TryParse(parola, out tryParse))
            {
                paroleRompiScatole.Add(parola);
            }
            numeroParole.Add(parola, parole.Where(p => p == parola).ToList().Count());
        }

        string currPath = System.IO.Directory.GetCurrentDirectory();
        //before your loop
        var csv = new StringBuilder();

        //after your loop
        File.WriteAllText($"{currPath}\\export\\parole_rompi.csv", String.Join(",\n", paroleRompiScatole));

        foreach (var np in numeroParole)
        {
            var newLine = string.Format("{0},{1}", np.Key, np.Value);
            csv.AppendLine(newLine);
        }

        File.WriteAllText($"{currPath}\\export\\dati_puliti.csv", String.Join(",", csv.ToString()));
    }

    public static void DoScript2(string pathCommenti, string pathParoleDaConfrontare)
    {
        List<string> parole = new List<string>();

        using (TextFieldParser parser = new TextFieldParser(pathParoleDaConfrontare))
        {
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    parole.Add(field.ToLower());
                }
            }
        }

        var csv = new StringBuilder();

        foreach (string parola in parole)
        {
            using (TextFieldParser parser = new TextFieldParser(pathCommenti))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    //Processing row
                    string[] fields = parser.ReadFields();

                    csv.AppendLine($"{parola},{fields[0]},{fields[1]},{fields[2]},{GetCommentSplitted(fields[3]).Where(p => p == parola).Count()}");

                }
            }
        }
        string currPath = System.IO.Directory.GetCurrentDirectory();

        File.WriteAllText($"{currPath}\\export\\dati_graph.csv", String.Join(",", csv.ToString()));
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
                        && p != ""
                        && !p.StartsWith("www")
                        )
                    .Select(p =>
                        CleanWord(p.ToLower())
                    ).ToList();
    }
}