using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

public class Program
{
    static void Main()
    {
        RensaSkarm();
        Console.OutputEncoding = Encoding.UTF8;
        SkrivRubrik("Hash Demo - Med och utan Salt");

        while (true)
        {
            Console.WriteLine();
            SkrivLabel("Välj ett alternativ:");
            Console.WriteLine("  1. Hasha text UTAN salt");
            Console.WriteLine("  2. Hasha text MED salt");
            Console.WriteLine("  3. Prestandatest - MD5, SHA-256, PBKDF2 och Argon2id");
            Console.WriteLine("  4. Avsluta");
            Console.WriteLine();
            SkrivPrompt("Ditt val: ");
            var val = Console.ReadLine();

            switch (val)
            {
                case "1":
                    HashUtanSalt();
                    break;
                case "2":
                    HashMedSalt();
                    break;
                case "3":
                    PrestandaTest();
                    break;
                case "4":
                    return;
                default:
                    // Kontrollera för easter egg hash
                    if (!string.IsNullOrEmpty(val) && KontrolleraEasterEggHash(val))
                    {
                        EasterEgg();
                    }
                    else
                    {
                        SkrivFelmeddelande("Ogiltigt val. Försök igen.");
                    }
                    break;
            }
        }
    }

    static void RensaSkarm()
    {
        // Console.Clear() kastar när utdata är omdirigerad, exempelvis i en pipeline.
        try { Console.Clear(); } catch (IOException) { }
    }

    static void SkrivRubrik(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=" + new string('=', text.Length + 2) + "=");
        Console.WriteLine("| " + text + " |");
        Console.WriteLine("=" + new string('=', text.Length + 2) + "=");
        Console.ResetColor();
    }

    static void SkrivLabel(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    static void SkrivPrompt(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(text);
        Console.ResetColor();
    }

    static void SkrivVärde(string label, string värde)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write(label);
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(värde);
        Console.ResetColor();
    }

    static void SkrivFelmeddelande(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    static void HashUtanSalt()
    {
        Console.WriteLine();
        SkrivPrompt("Ange text att hasha: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }
        string hash = BeraknaHash(input);
        Console.WriteLine();
        SkrivVärde("Hash (utan salt): ", hash);
    }

    static void HashMedSalt()
    {
        Console.WriteLine();
        SkrivPrompt("Ange text att hasha: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }
        string salt = SkapaSalt(16);
        string hash = BeraknaHash(input + salt);
        Console.WriteLine();
        SkrivVärde("Salt: ", salt);
        SkrivVärde("Hash (med salt): ", hash);
    }

    // ===== PRESTANDATEST: snabba hashar mot nyckelderiveringsfunktioner =====

    /// <summary>
    /// Arbetskostnad för de två nyckelderiveringsfunktionerna. Riktvärdet i produktion
    /// är 250 till 500 millisekunder per inloggning, mätt på produktionshårdvaran.
    /// </summary>
    sealed record Arbetskostnad(
        string Namn,
        int Pbkdf2Iterationer,
        int Argon2MinneKib,
        int Argon2Iterationer);

    static readonly Arbetskostnad[] Nivåer =
    [
        new("1. Låg      (för svag 2026)", 100_000, 8 * 1024, 1),
        new("2. Standard (OWASP-rekommendation)", 600_000, 19 * 1024, 2),
        new("3. Hög      (känsliga system)", 1_200_000, 64 * 1024, 3)
    ];

    static void PrestandaTest()
    {
        Console.WriteLine();
        SkrivLabel("Prestandatest - hur dyr är en gissning för en angripare?");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("MD5 och SHA-256 är byggda för att vara snabba.");
        Console.WriteLine("PBKDF2 och Argon2id är byggda för att vara långsamma. Det är hela poängen.");
        Console.ResetColor();
        Console.WriteLine();

        SkrivPrompt("Ange text att testa (exempelvis ett lösenord): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }

        SkrivPrompt("Antal iterationer för de snabba hasharna (rekommenderat: 10000): ");
        if (!int.TryParse(Console.ReadLine(), out int iterations) || iterations <= 0)
        {
            iterations = 10_000;
            Console.WriteLine($"Använder standardvärde: {iterations} iterationer");
        }

        Console.WriteLine();
        SkrivLabel("Välj arbetskostnad för PBKDF2 och Argon2id:");
        foreach (var nivå in Nivåer)
        {
            Console.WriteLine($"  {nivå.Namn}");
        }
        Console.WriteLine();
        SkrivPrompt("Nivå (1-3, standard 2): ");
        if (!int.TryParse(Console.ReadLine(), out int nivåVal) || nivåVal < 1 || nivåVal > 3)
        {
            nivåVal = 2;
        }
        var kostnad = Nivåer[nivåVal - 1];

        byte[] salt = RandomNumberGenerator.GetBytes(16);

        Console.WriteLine();
        SkrivLabel("Startar prestandatest, Argon2id tar några sekunder...");
        Console.WriteLine();

        // Konverteringen till byte ligger utanför mätningen. Annars mäter vi
        // stränghanteringen i stället för hashningen, den är dyrare än MD5.
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        double md5PerHash = MätSnabbHash(() => MD5.HashData(inputBytes), iterations);
        double shaPerHash = MätSnabbHash(() => SHA256.HashData(inputBytes), iterations);

        // Nyckelderiveringsfunktionerna körs bara några gånger. De är så dyra att
        // 10 000 varv skulle ta timmar, vilket i sig är hela poängen med dem.
        const int kdfKörningar = 5;
        double pbkdf2PerHash = MätSnabbHash(
            () => Rfc2898DeriveBytes.Pbkdf2(input, salt, kostnad.Pbkdf2Iterationer, HashAlgorithmName.SHA256, 32),
            kdfKörningar);
        double argon2PerHash = MätSnabbHash(
            () => Argon2idHash(input, salt, kostnad.Argon2MinneKib, kostnad.Argon2Iterationer),
            kdfKörningar);

        SkrivLabel($"Resultat (arbetskostnad: {kostnad.Namn.Trim()})");
        Console.WriteLine();
        Console.WriteLine("  Algoritm     Tid per hash          Gissningar per sekund (en kärna)");
        Console.WriteLine("  ----------------------------------------------------------------");
        SkrivRad("MD5", md5PerHash);
        SkrivRad("SHA-256", shaPerHash);
        SkrivRad($"PBKDF2", pbkdf2PerHash, $"{kostnad.Pbkdf2Iterationer:N0} iterationer");
        SkrivRad("Argon2id", argon2PerHash, $"{kostnad.Argon2MinneKib / 1024} MiB minne, {kostnad.Argon2Iterationer} pass");

        Console.WriteLine();
        double faktor = argon2PerHash / md5PerHash;
        SkrivVärde("DIN SIFFRA: Argon2id är ", $"{faktor:N0} gånger långsammare än MD5 på den här maskinen");
        SkrivVärde("Hårdvara: ", $"{Environment.ProcessorCount} logiska kärnor, {RuntimeInformationText()}");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Så här läser du siffran:");
        Console.WriteLine("  En angripare med er läckta databas gissar offline, utan spärrar.");
        Console.WriteLine($"  Med MD5 hinner en enda kärna {(1000 / md5PerHash):N0} gissningar i sekunden.");
        Console.WriteLine($"  Med Argon2id hinner samma kärna {(1000 / argon2PerHash):N1}.");
        Console.WriteLine("  Ett grafikkort gör MD5 tusentals gånger snabbare än så, men bromsas");
        Console.WriteLine("  av Argon2id eftersom varje gissning kräver megabyte av arbetsminne.");
        Console.ResetColor();

        if (argon2PerHash < 250 || argon2PerHash > 500)
        {
            Console.WriteLine();
            SkrivLabel($"Riktvärdet är 250 till 500 ms per inloggning. Din Argon2id landade på {argon2PerHash:F0} ms.");
            Console.WriteLine("  Kör testet igen med en annan nivå och se hur tiden följer med.");
        }
    }

    static void SkrivRad(string namn, double msPerHash, string? parametrar = null)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write($"  {namn,-12} ");
        Console.ForegroundColor = ConsoleColor.White;
        string tid = msPerHash < 1
            ? $"{msPerHash * 1000:F1} mikrosekunder"
            : $"{msPerHash:F1} millisekunder";
        Console.Write($"{tid,-21} ");
        Console.WriteLine($"{(1000 / msPerHash),18:N0}");
        Console.ResetColor();
        if (parametrar is not null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"               ({parametrar})");
            Console.ResetColor();
        }
    }

    static double MätSnabbHash(Action arbete, int antal)
    {
        arbete(); // Ett varv först, så att JIT-kompileringen inte hamnar i mätningen.
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < antal; i++)
        {
            arbete();
        }
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds / antal;
    }

    static byte[] Argon2idHash(string lösenord, byte[] salt, int minneKib, int iterationer)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(lösenord))
        {
            Salt = salt,
            MemorySize = minneKib,
            Iterations = iterationer,
            DegreeOfParallelism = 1
        };
        return argon2.GetBytes(32);
    }

    static string RuntimeInformationText()
        => $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim()}, .NET {Environment.Version}";

    public static string BeraknaHash(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    static string SkapaSalt(int längd)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(längd);
        return Convert.ToBase64String(saltBytes);
    }

    public static bool KontrolleraEasterEggHash(string input)
    {
        // Obfuskerad hash - XOR-kodad med nyckel
        byte[] kodadBytes = {
            0x6f, 0x2b, 0xe5, 0xb2, 0xaf, 0x5c, 0xda, 0xad,
            0x88, 0x70, 0x6e, 0x9b, 0x95, 0x43, 0x12, 0x15,
            0x2b, 0x7b, 0x4c, 0x9d, 0x0f, 0xfe, 0x0b, 0xdc,
            0xbe, 0xdf, 0x3b, 0xa9, 0x87, 0xdc, 0x0b, 0xfc
        };

        byte[] nyckel = {
            0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65,
            0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65,
            0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65,
            0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65, 0x65
        };

        // Dekoda genom XOR
        byte[] hashBytes = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            hashBytes[i] = (byte)(kodadBytes[i] ^ nyckel[i]);
        }

        // Konvertera till hex-sträng
        string målHash = Convert.ToHexStringLower(hashBytes);

        // Kontrollera endast om användaren skrev hela hashen exakt
        return input.ToLower() == målHash;
    }

    static void EasterEgg()
    {
        RensaSkarm();

        // Animerad rubrik
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🎉 GRATTIS! Du hittade det hemliga easter egget! 🎉");
        Console.ResetColor();

        System.Threading.Thread.Sleep(1000);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("════════════════════════════════════════════════");
        Console.WriteLine("║              🔓 HASH MASTER MODE 🔓            ║");
        Console.WriteLine("════════════════════════════════════════════════");
        Console.ResetColor();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Du har låst upp den hemliga Hash Master-funktionen!");
        Console.WriteLine("Här kan du utforska avancerade hash-funktioner...");
        Console.ResetColor();

        Console.WriteLine();

        while (true)
        {
            SkrivLabel("🎯 Hash Master Meny:");
            Console.WriteLine("  1. 🌈 Regnbågs-hash (olika algoritmer)");
            Console.WriteLine("  2. 🎲 Hash-gissning (gissa rätt input)");
            Console.WriteLine("  3. 🔍 Lavineffekt: en bokstav ändrar allt");
            Console.WriteLine("  4. 🚪 Tillbaka till huvudmenyn");
            Console.WriteLine();

            SkrivPrompt("Välj din hash-magi: ");
            var val = Console.ReadLine();

            switch (val)
            {
                case "1":
                    RegnbågsHash();
                    break;
                case "2":
                    HashGissning();
                    break;
                case "3":
                    Lavineffekt();
                    break;
                case "4":
                    RensaSkarm();
                    SkrivRubrik("Hash Demo - Med och utan Salt");
                    return;
                default:
                    SkrivFelmeddelande("🤔 Hmm, det där förstod jag inte. Försök igen!");
                    break;
            }
        }
    }

    static void RegnbågsHash()
    {
        Console.WriteLine();
        SkrivPrompt("🌈 Ange text för regnbågs-hash: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(input);

        Console.WriteLine();
        SkrivLabel("🎨 Samma text genom fyra olika hashfunktioner:");

        SkrivHashRad(ConsoleColor.Red, "🔴 MD5     ", Convert.ToHexStringLower(MD5.HashData(bytes)), "trasig, kollisioner på sekunder");
        SkrivHashRad(ConsoleColor.Yellow, "🟡 SHA-1   ", Convert.ToHexStringLower(SHA1.HashData(bytes)), "trasig sedan SHAttered 2017");
        SkrivHashRad(ConsoleColor.Green, "🟢 SHA-256 ", Convert.ToHexStringLower(SHA256.HashData(bytes)), "stark, men för snabb för lösenord");
        SkrivHashRad(ConsoleColor.Blue, "🔵 SHA-512 ", Convert.ToHexStringLower(SHA512.HashData(bytes)), "stark, dubbelt så lång utdata");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Lägg märke till att längden är fast oavsett hur lång texten är.");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void SkrivHashRad(ConsoleColor färg, string namn, string hash, string kommentar)
    {
        Console.ForegroundColor = färg;
        Console.Write(namn + ": ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(hash);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"             {kommentar}");
        Console.ResetColor();
    }

    static void HashGissning()
    {
        Console.WriteLine();
        SkrivLabel("🎲 Hash-gissning: Kan du gissa rätt input?");

        string[] hemligheter = { "katt", "hund", "fisk", "fågel", "häst", "ko", "får", "gris", "mus", "råtta" };
        // RandomNumberGenerator, inte Random. I en säkerhetskurs väljer vi rätt slump även i ett spel.
        string hemligInput = hemligheter[RandomNumberGenerator.GetInt32(hemligheter.Length)];
        string målHash = BeraknaHash(hemligInput);

        Console.WriteLine();
        SkrivVärde("🎯 Mål-hash (första 12 tecken): ", målHash.Substring(0, 12) + "...");
        SkrivLabel("💡 Ledtråd: Det är ett djur (på svenska, gemener)");

        int försök = 0;
        int maxFörsök = 5;

        while (försök < maxFörsök)
        {
            Console.WriteLine();
            SkrivPrompt($"Gissning {försök + 1}/{maxFörsök}: ");
            string? gissning = Console.ReadLine();

            if (string.IsNullOrEmpty(gissning))
            {
                SkrivFelmeddelande("Tom gissning räknas inte!");
                continue;
            }

            försök++;
            string gissningHash = BeraknaHash(gissning.ToLower());

            if (gissningHash == målHash)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("🎉 RÄTT! Du är en sann Hash Master!");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⭐ Du gissade rätt på {försök} försök!");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Notera hur du gjorde det: du gissade en klartext och jämförde hashar.");
                Console.WriteLine("Det är exakt så en angripare knäcker en läckt lösenordsdatabas.");
                Console.ResetColor();
                return;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("❌ Fel! ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Din hash: {gissningHash.Substring(0, 12)}...");
                Console.ResetColor();

                if (försök == maxFörsök)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"💔 Slut på försök! Rätt svar var: {hemligInput}");
                    Console.ResetColor();
                }
            }
        }
    }

    static void Lavineffekt()
    {
        Console.WriteLine();
        SkrivLabel("🔍 Lavineffekt: hur mycket ändras av en enda bokstav?");
        Console.WriteLine();

        SkrivPrompt("Ange en text: ");
        string? a = Console.ReadLine();
        if (string.IsNullOrEmpty(a))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }

        SkrivPrompt("Ange nästan samma text, ändra en enda bokstav: ");
        string? b = Console.ReadLine();
        if (string.IsNullOrEmpty(b))
        {
            SkrivFelmeddelande("Ingen text angavs.");
            return;
        }

        byte[] hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        byte[] hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b));

        Console.WriteLine();
        SkrivVärde($"SHA-256 av \"{a}\": ", Convert.ToHexStringLower(hashA));
        SkrivVärde($"SHA-256 av \"{b}\": ", Convert.ToHexStringLower(hashB));

        int olikaBitar = 0;
        for (int i = 0; i < hashA.Length; i++)
        {
            olikaBitar += System.Numerics.BitOperations.PopCount((uint)(hashA[i] ^ hashB[i]));
        }

        Console.WriteLine();
        SkrivVärde("Antal bitar som skiljer: ", $"{olikaBitar} av 256 ({olikaBitar / 2.56:F0} procent)");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("En bra hashfunktion ändrar ungefär hälften av bitarna, oavsett hur");
        Console.WriteLine("liten ändringen i indata är. Det gör att hashen inte avslöjar något");
        Console.WriteLine("om hur nära din gissning låg.");
        Console.ResetColor();
        Console.WriteLine();
    }
}
