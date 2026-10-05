using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string delimitator = " ";

        string caleStopwords = File.Exists("stopwords.txt") ? "stopwords.txt" : "../stopwords.txt";
        string bazaReuters = Directory.Exists("Reuters") ? "Reuters" : "../Reuters";
        string folderIesire = Directory.Exists("Reuters") ? "Iesire" : "../Iesire";
        Directory.CreateDirectory(folderIesire);

        var stopwords = new HashSet<string>(File.ReadAllLines(caleStopwords).Select(w => w.Trim().ToLower()));
        var stemmer = new WordFrequency.PorterStemmer();

        string[] foldere = { "Training", "Testing" };

        foreach (var folder in foldere)
        {
            string caleFolder = Path.Combine(bazaReuters, "Reuters_34", folder);
            string subfolderIesire = Path.Combine(folderIesire, folder);
            Directory.CreateDirectory(subfolderIesire);

            foreach (var fisier in Directory.GetFiles(caleFolder, "*.XML"))
            {
                var doc = XDocument.Load(fisier);

                // Titlu
                string titlu = doc.Descendants("title").FirstOrDefault()?.Value.Trim() ?? "";

                // Paragrafe din <text>
                string paragrafe = string.Join(" ", doc.Descendants("p").Select(p => p.Value));

                // Metadata class
                var categorii = doc.Descendants("codes")
                    .Where(c => (string?)c.Attribute("class") == "bip:topics:1.0")
                    .Descendants("code")
                    .Select(c => (string?)c.Attribute("code") ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                // Normalizare (fara uppercase, semne, cifre sau stopwords)
                string textComplet = titlu + " " + paragrafe;
                var caractere = textComplet.Select(c => char.IsLetter(c) ? char.ToLower(c) : ' ').ToArray();
                var cuvinte = new string(caractere)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 1 && !stopwords.Contains(w))
                    .Select(w => stemmer.StemWord(w));

                string textFinal = string.Join(delimitator, cuvinte);

                // Salvare in fisier separat
                string numeFisier = Path.GetFileNameWithoutExtension(fisier) + ".txt";
                string caleFisierIesire = Path.Combine(subfolderIesire, numeFisier);

                string continutFisier = "Titlu: " + titlu + Environment.NewLine +
                                        "Categorii: " + string.Join(" ", categorii) + Environment.NewLine +
                                        "Cuvinte: " + textFinal;
                File.WriteAllText(caleFisierIesire, continutFisier);
            }
        }
    }
}
