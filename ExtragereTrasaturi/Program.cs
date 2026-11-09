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
    public Dictionary<int, double> VectorTfIdf = new Dictionary<int, double>();
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
        
        /*
        // Calcul IDF
        int totalDoc = articole.Count;
        Dictionary<int, int> aparitiiInDoc = new Dictionary<int, int>();

        foreach (var art in articole)
        {
            HashSet<int> indiciUnici = new HashSet<int>();
            foreach (var w in art.Cuvinte)
            {
                indiciUnici.Add(indexCuvinte[w]);
            }

            foreach (var idx in indiciUnici)
            {
                if (!aparitiiInDoc.ContainsKey(idx))
                    aparitiiInDoc[idx] = 0;
                aparitiiInDoc[idx]++;
            }
        }

        Dictionary<int, double> idf = new Dictionary<int, double>();
        foreach (var intrare in aparitiiInDoc)
        {
            idf[intrare.Key] = Math.Log((double)totalDoc / intrare.Value);
        }

        // TF-IDF pentru articole
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

            foreach (var intrare in frecvente)
            {
                int idx = intrare.Key;
                int tf = intrare.Value;
                art.VectorTfIdf[idx] = tf * idf[idx];
            }
        }

        // Citire si procesare interogare
        Console.Write("Introdu interogarea: ");
        string interogare = Console.ReadLine() ?? "";

        var caractereQ = interogare.Select(c => char.IsLetter(c) ? char.ToLower(c) : ' ').ToArray();
        var cuvinteQ = new string(caractereQ)
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && !stopwords.Contains(w))
            .Select(w => stemmer.StemWord(w))
            .ToList();

        Dictionary<int, int> frecventeQ = new Dictionary<int, int>();
        foreach (var w in cuvinteQ)
        {
            if (indexCuvinte.ContainsKey(w))
            {
                int idx = indexCuvinte[w];
                if (!frecventeQ.ContainsKey(idx))
                    frecventeQ[idx] = 0;
                frecventeQ[idx]++;
            }
        }

        Dictionary<int, double> vectorQ = new Dictionary<int, double>();
        foreach (var intrare in frecventeQ)
        {
            int idx = intrare.Key;
            int tf = intrare.Value;
            vectorQ[idx] = tf * idf[idx];
        }

        // Calcul similaritate Cosinus
        double sumaPatrateQ = 0;
        foreach (var val in vectorQ.Values)
            sumaPatrateQ += val * val;
        double normaQ = Math.Sqrt(sumaPatrateQ);

        List<(string Nume, double Scor)> rezultate = new List<(string Nume, double Scor)>();

        foreach (var art in articole)
        {
            double dotProduct = 0;
            foreach (var intrare in vectorQ)
            {
                int idx = intrare.Key;
                if (art.VectorTfIdf.ContainsKey(idx))
                {
                    dotProduct += intrare.Value * art.VectorTfIdf[idx];
                }
            }

            double sumaPatrateArt = 0;
            foreach (var val in art.VectorTfIdf.Values)
                sumaPatrateArt += val * val;
            double normaArt = Math.Sqrt(sumaPatrateArt);

            double similaritate = (normaQ > 0 && normaArt > 0) ? dotProduct / (normaQ * normaArt) : 0;
            rezultate.Add((art.Nume, similaritate));
        }

        // Ordonare si afisare top 5
        var topArticole = rezultate.OrderByDescending(r => r.Scor).Take(5);

        Console.WriteLine("\nDocumentele cele mai similare:");
        foreach (var r in topArticole)
        {
            Console.WriteLine(r.Nume + " - Similaritate: " + r.Scor.ToString("F4"));
        }
        */
        
    }
}
