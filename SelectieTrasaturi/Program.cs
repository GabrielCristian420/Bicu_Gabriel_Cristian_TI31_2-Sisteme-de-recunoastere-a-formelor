using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

class Articol
{
    public HashSet<int> AtributePrezente = new HashSet<int>();
    public List<string> Categorii = new List<string>();
}

class Program
{
    static void Main()
    {
        string caleIntrare = File.Exists("Iesire/vectori_rar.txt") ? "Iesire/vectori_rar.txt" : "../Iesire/vectori_rar.txt";
        string folderIesire = Directory.Exists("Iesire") ? "Iesire" : "../Iesire";
        string caleIesire = Path.Combine(folderIesire, "vectori_selectie.txt");

        List<string> atribute = new List<string>();
        List<Articol> articole = new List<Articol>();
        List<string> liniiDateOriginale = new List<string>();

        bool esteSectiuneDate = false;

        // Indicii pornesc de la 0 in vectori_rar.txt
        foreach (var linie in File.ReadAllLines(caleIntrare))
        {
            if (linie.StartsWith("@attribute"))
            {
                var parti = linie.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parti.Length >= 2)
                    atribute.Add(parti[1]);
            }
            else if (linie.StartsWith("@data"))
            {
                esteSectiuneDate = true;
            }
            else if (esteSectiuneDate)
            {
                liniiDateOriginale.Add(linie);

                var parti = linie.Split('#');
                if (parti.Length >= 3)
                {
                    Articol art = new Articol();

                    // Fiecare categorie este luata in considerare individual
                    var categorii = parti[2].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var c in categorii)
                    {
                        art.Categorii.Add(c);
                    }

                    var perechi = parti[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pereche in perechi)
                    {
                        var partiPereche = pereche.Split(':');
                        if (partiPereche.Length >= 2 && int.TryParse(partiPereche[0], out int idx) && int.TryParse(partiPereche[1], out int frecv) && frecv > 0)
                        {
                            art.AtributePrezente.Add(idx);
                        }
                    }

                    articole.Add(art);
                }
            }
        }

        double entropieGenerala = CalculeazaEntropie(articole);
        List<double> castigInformational = new List<double>();
        int totalArticole = articole.Count;

        for (int i = 0; i < atribute.Count; i++)
        {
            List<Articol> artCuAtribut = new List<Articol>();
            List<Articol> artFaraAtribut = new List<Articol>();

            foreach (var art in articole)
            {
                if (art.AtributePrezente.Contains(i))
                    artCuAtribut.Add(art);
                else
                    artFaraAtribut.Add(art);
            }

            double entropieCu = CalculeazaEntropie(artCuAtribut);
            double entropieFara = CalculeazaEntropie(artFaraAtribut);

            // Gain(S, A) = Entropy(S) - (|S1|/|S|)*Entropy(S1) - (|S0|/|S|)*Entropy(S0)
            double ig = entropieGenerala 
                        - ((double)artCuAtribut.Count / totalArticole) * entropieCu 
                        - ((double)artFaraAtribut.Count / totalArticole) * entropieFara;

            castigInformational.Add(ig);
        }

        List<string> liniiIesire = new List<string>();

        for (int i = 0; i < atribute.Count; i++)
        {
            liniiIesire.Add("@attribute " + atribute[i] + " " + castigInformational[i].ToString("F6", CultureInfo.InvariantCulture));
        }

        liniiIesire.Add("@data");
        liniiIesire.AddRange(liniiDateOriginale);

        File.WriteAllLines(caleIesire, liniiIesire);

        Console.WriteLine("Etapa 1 finalizata cu succes!");
        Console.WriteLine("Fisier generat: " + caleIesire);
    }

    // Fiecare articol are greutatea 1 si o imparte egal intre categoriile lui
    static double CalculeazaEntropie(List<Articol> articole)
    {
        if (articole.Count == 0)
            return 0;

        Dictionary<string, double> distributieClase = new Dictionary<string, double>();

        foreach (var art in articole)
        {
            if (art.Categorii.Count == 0)
                continue;

            double pondere = 1.0 / art.Categorii.Count;
            foreach (var c in art.Categorii)
            {
                if (!distributieClase.ContainsKey(c))
                    distributieClase[c] = 0;
                distributieClase[c] += pondere;
            }
        }

        double entropie = 0;
        int totalArticole = articole.Count;

        foreach (var valoare in distributieClase.Values)
        {
            double p = valoare / totalArticole;
            if (p > 0)
                entropie -= p * Math.Log2(p);
        }

        return entropie;
    }
}
