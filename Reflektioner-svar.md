Var måste saltet lagras?
Saltet sparas i databasen tillsammans med hashen, kopplat till användaren (typiskt i en kolumn bredvid). Det måste finnas kvar, för vid nästa inloggning behöver servern exakt samma salt: den lägger saltet till det inskrivna lösenordet, hashar, och jämför med den sparade hashen. Utan det sparade saltet blir hashen en annan och ingen kan logga in.

Varför är det inte ett problem att saltet ligger i klartext?
För att saltets uppgift inte är att vara hemligt — det är att göra varje hash unik. Även om en angripare får saltet när databasen läcker, ger det ingen genväg. Poängen är att ett unikt salt per användare omöjliggör förberäknade attacker (rainbow tables) och gör att arbetet inte kan återanvändas mellan användare: varje lösenord måste angripas för sig, ett i taget.

🎉 GRATTIS! Du hittade det hemliga easter egget! 🎉

════════════════════════════════════════════════
║              🔓 HASH MASTER MODE 🔓            ║
════════════════════════════════════════════════

Du har låst upp den hemliga Hash Master-funktionen!
Här kan du utforska avancerade hash-funktioner...

🎯 Hash Master Meny:
  1. 🌈 Regnbågs-hash (olika algoritmer)
  2. 🎲 Hash-gissning (gissa rätt input)
  3. 🔍 Lavineffekt: en bokstav ändrar allt
  4. 🚪 Tillbaka till huvudmenyn

Välj din hash-magi:

skriv tre till fem meningar som besvarar: varför ger salt olika resultat och varför är det bra, vad visade din mätning och hur skulle du lagra lösenord i en riktig .NET-applikation?
Salt ger olika resultat eftersom ett unikt, slumpat salt läggs till varje lösenord innan det hashas, så att även identiska lösenord får olika hashar. Det är bra för att det omöjliggör förberäknade attacker som rainbow-tabeller — angriparen kan inte återanvända arbetet mellan användare, utan tvingas knäcka varje lösenord för sig.

Min mätning visade att Argon2id är över 1,5 miljoner gånger långsammare än MD5 på min maskin (24 logiska kärnor, .NET 10). Det betyder att för en angripare med en läckt databas blir varje gissning mot Argon2id över en miljon gånger dyrare än mot MD5 — det som är en omärkbar bråkdels sekund för en inloggande användare blir en mur för någon som kör miljardtals gissningar offline.

I en riktig .NET-applikation skulle jag lagra lösenord genom att hasha dem med Argon2id, med ett unikt slumpat salt per lösenord som sparas tillsammans med hashen i databasen. Jag skulle inte implementera hashningen själv, utan använda ett beprövat ramverksstöd — ASP.NET Core Identitys PasswordHasher<T>, eller Konscious.Security.Cryptography.Argon2 för Argon2id specifikt.