# Hash Demo

En konsolapplikation i .NET som visar tre saker: vad ett salt gör, varför ett lösenord ska lagras långsamt och hur du mäter arbetskostnaden själv.

Används i Övning 3.2 i kursen IT-säkerhet för utvecklare.

## Krav

- .NET 10 SDK
- Windows, macOS eller Linux
- Internetanslutning vid första bygget, projektet hämtar NuGet-paketet för Argon2id

## Kom igång

```bash
git clone https://github.com/QuBaR/HashDemo.git
cd HashDemo
dotnet run --configuration Release
```

Kör i Release. I Debug är optimeringarna avstängda och mätningarna i menyval 3 blir missvisande.

## Menyn

| Val | Vad det gör |
|-----|-------------|
| 1 | Hashar en text med SHA-256, utan salt. Samma text ger alltid samma hash |
| 2 | Hashar en text med SHA-256 och ett slumpat salt. Samma text ger olika hash varje gång |
| 3 | Prestandatest: MD5, SHA-256, PBKDF2 och Argon2id, med valbar arbetskostnad |
| 4 | Avslutar |

### Menyval 3, prestandatestet

Testet frågar efter tre saker: en text, hur många varv de snabba hasharna ska köras och vilken arbetskostnad nyckelderiveringsfunktionerna ska använda.

De tre nivåerna är:

| Nivå | PBKDF2 | Argon2id |
|------|--------|----------|
| 1, låg | 100 000 iterationer | 8 MiB minne, 1 pass |
| 2, standard | 600 000 iterationer | 19 MiB minne, 2 pass |
| 3, hög | 1 200 000 iterationer | 64 MiB minne, 3 pass |

Nivå 2 motsvarar OWASP:s rekommendation i Password Storage Cheat Sheet.

MD5 och SHA-256 körs det antal varv du angav. PBKDF2 och Argon2id körs bara fem gånger, eftersom 10 000 varv skulle ta timmar. Det är i sig hela poängen med dem.

Resultatet är en siffra: hur många gånger långsammare Argon2id är än MD5 på just din maskin. Den siffran är övningens viktigaste resultat och den betyder ingenting utan uppgift om vilken hårdvara du körde på.

## Två saker som brukar överraska i mätningen

**SHA-256 kan bli snabbare än MD5.** Moderna processorer har SHA-instruktioner inbyggda i hårdvaran, MD5 har ingen sådan acceleration. MD5 är alltså inte trasigt för att det är långsamt, det är trasigt för att kollisioner går att framställa.

**PBKDF2 kan ta längre tid än Argon2id.** Väggklockan berättar bara halva historien. Argon2id kostar dessutom minne, flera megabyte per gissning, och det är minneskravet som stoppar grafikkorten. Ett kort med tusentals kärnor kan bara köra så många parallella gissningar som minnet räcker till.

## Teknisk information

- Snabba hashfunktioner: `MD5`, `SHA256`, `SHA1` och `SHA512` från `System.Security.Cryptography`
- Nyckelderivering: `Rfc2898DeriveBytes.Pbkdf2` (PBKDF2-SHA256) och `Konscious.Security.Cryptography.Argon2id`
- Saltlängd: 16 byte, slumpat med `RandomNumberGenerator`
- Mätning: `Stopwatch`, med ett uppvärmningsvarv före varje mätning så att JIT-kompileringen inte hamnar i siffrorna

## Felsökning

**`dotnet: kommandot hittades inte`**
Installera .NET SDK från https://dotnet.microsoft.com/download

**Bygget klagar på Konscious.Security.Cryptography.Argon2**
Kör `dotnet restore` med internetanslutning. Paketet hämtas från nuget.org vid första bygget.

**Inga färger i konsolen**
Använd en modern terminal, exempelvis Windows Terminal.

**Orimliga siffror i menyval 3**
Du kör troligen i Debug. Starta om med `dotnet run --configuration Release`.

---

## Hash Master Mode

Det finns en dold meny för den som löser gåtan.

**Fråga:** Vilket smeknamn passar bäst för en utvecklare som är vass på säker lösenordslagring med hashning och salting?

**Ledtrådar:**
- Börjar med ordet för processen: `hash`
- Slutar med engelska ordet för mästare
- Ett ord, tio bokstäver, skrivs med gemener

**Så låser du upp menyn:**

1. Lista ut ordet
2. Välj menyval 1, Hasha text UTAN salt
3. Mata in ordet
4. Kopiera hela hashvärdet som skrivs ut
5. Gå tillbaka till huvudmenyn och klistra in hashvärdet som menyval

I Hash Master Mode hittar du regnbågs-hash som visar samma text genom fyra hashfunktioner, ett gissningsspel som fungerar precis som en angripares ordlisteattack och en lavineffektsmätning som räknar hur många bitar som ändras när du byter en enda bokstav.

## Varför fungerar den här gåtan?

Målhashen ligger XOR-kodad i källkoden i stället för som en läsbar sträng. Det gör den jobbigare att hitta, men inte omöjlig: koden som avkodar den ligger tre rader längre ner och en kompilerad assembly går att öppna i ILSpy.

Det är precis den poängen kursen gör i Del 3. Obfuskering köper tid, den skapar inte säkerhet. En hemlighet som måste finnas i klienten för att programmet ska fungera är i praktiken publicerad.

**Author:** Robert Jansz, https://github.com/QuBaR/
