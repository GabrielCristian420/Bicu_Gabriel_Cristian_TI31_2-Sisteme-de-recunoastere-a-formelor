using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

class Articol
{
    public string Nume = "";
    public string Folder = "";
    public List<string> Categorii = new List<string>();
    public List<string> Cuvinte = new List<string>();
}

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

        List<string> dictionarGlobal = new List<string>();
        Dictionary<string, int> indexCuvinte = new Dictionary<string, int>();
        List<Articol> articole = new List<Articol>();

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
                    .Select(w => stemmer.StemWord(w))
                    .ToList();

                foreach (var w in cuvinte)
                {
                    if (!indexCuvinte.ContainsKey(w))
                    {
                        indexCuvinte[w] = dictionarGlobal.Count;
                        dictionarGlobal.Add(w);
                    }
                }

                string numeFisier = Path.GetFileNameWithoutExtension(fisier);
                articole.Add(new Articol
                {
                    Nume = numeFisier,
                    Folder = folder,
                    Categorii = categorii,
                    Cuvinte = cuvinte
                });

                // Salvare in fisier separat
                string caleFisierIesire = Path.Combine(subfolderIesire, numeFisier + ".txt");
                string continutFisier = "Titlu: " + titlu + Environment.NewLine +
                                        "Categorii: " + string.Join(" ", categorii) + Environment.NewLine +
                                        "Cuvinte: " + string.Join(delimitator, cuvinte);
                File.WriteAllText(caleFisierIesire, continutFisier);
            }
        }

        // Salvare vectori rar
        List<string> linii = new List<string>();

        foreach (var w in dictionarGlobal)
        {
            linii.Add("@attribute " + w);
        }

        linii.Add("@data");

        foreach (var art in articole)
        {
            Dictionary<int, int> frecvente = new Dictionary<int, int>();
            foreach (var w in art.Cuvinte)
            {
                int idx = indexCuvinte[w];
                if (!frecvente.ContainsKey(idx))
                    frecvente[idx] = 0;
                frecvente[idx]++;
            }

            var vectorRar = frecvente.OrderBy(x => x.Key).Select(x => x.Key + ":" + x.Value);
            string linieVector = string.Join(" ", vectorRar);
            string categoriiStr = string.Join(" ", art.Categorii);

            linii.Add(art.Nume + " # " + linieVector + " # " + categoriiStr + " # " + art.Folder);
        }

        string fisierVectori = Path.Combine(folderIesire, "vectori_rar.txt");
        File.WriteAllLines(fisierVectori, linii);
    }
}
