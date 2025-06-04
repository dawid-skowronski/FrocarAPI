# FrocarAPI
## Funkcjonalności

| ID        | Opis funkcjonalności                                      | API | Web | Mobile | Desktop |
|-----------|----------------------------------------------------------|-----|-----|--------|---------|
| RENT-01   | Pierwszy administrator jest automatycznie dodany do systemu. | ✓   |     |        |         |
| RENT-02   | Administrator może zalogować się w systemie.            | *   |     |        | ✓       |
| RENT-03   | Administrator może dodawać pojazdy do systemu.          | *   |     |        | ✓       |
| RENT-04   | Administrator może edytować i usuwać pojazdy.           | *   |     |        | ✓       |
| RENT-05   | Administrator może zatwierdzać i usuwać ogłoszenia użytkowników. | *   |     |        | ✓       |
| RENT-06   | Administrator widzi statystyki systemowe.               | *   |     |        | ✓       |
| RENT-07   | Użytkownik może przeglądać dostępne pojazdy.            | *   | ✓   | ✓      |         |
| RENT-08   | Użytkownik może filtrować pojazdy według kategorii, ceny i lokalizacji. | *   | ✓   | ✓      |         |
| RENT-09   | Użytkownik może zobaczyć lokalizację pojazdu na mapie.  | *   | ✓   | ✓      |         |
| RENT-10   | Użytkownik może zarejestrować się w systemie.           | *   | ✓   | ✓      |         |
| RENT-11   | Użytkownik może zalogować się w systemie.               | *   | ✓   | ✓      |         |
| RENT-12   | Użytkownik może zresetować swoje hasło.                 | *   | ✓   | ✓      |         |
| RENT-13   | Użytkownik może wynająć pojazd na określony czas.       | *   | ✓   | ✓      |         |
| RENT-14   | Użytkownik otrzymuje powiadomienia o statusie rezerwacji. | *   | ✓   | ✓      |         |
| RENT-15   | Użytkownik widzi historię swoich wynajmów.              | *   | ✓   | ✓      |         |
| RENT-16   | Właściciel pojazdu może dodać swój pojazd do wynajmu.   | *   | ✓   | ✓      |         |
| RENT-17   | Właściciel może edytować i usuwać swoje ogłoszenia.     | *   | ✓   | ✓      |         |
| RENT-18   | Użytkownik może oceniać i recenzować wynajęte pojazdy.  | *   | ✓   | ✓      |         |
| RENT-19   | Użytkownik może się wylogować.                          |     | ✓   | ✓      |         |

Lokalne uruchomienie aplikacji API
1. Wymagania wstępne:<br>
• Zainstaluj Visual Studio 2022 (zalecana wersja Community, Professional)
2. Pobieranie kodu:<br>
• Sklonuj repozytorium projektu: https://github.com/dawid-skowronski/FrocarAPI lub pobierz plik FrogCar.
3. Wczytanie projektu:<br>
• Otwórz plik rozwiązania (FrogCar.sln) w Visual Studio 2022.
4. Konfiguracja:<br>
• Upewnij się, że wszystkie zależności NuGet (np.Microsoft.EntityFrameworkCore.SqlServer), są poprawnie zainstalowane.
Wykonaj jedną z poniższych metod:<br>
– W konsoli systemowej: Przejdź do katalogu projektu i wykonaj komendę dotnet restore w terminalu, aby pobrać wszystkie pakiety NuGet.<br>
– W konsoli menedżera pakietów: W Visual Studio otwórz konsolę menedżera pakietów (Package Manager Console) i wykonaj komendę Restore-Package.
5. Migracje bazy danych:<br>
• W terminalu w Visual Studio (Package Manager Console) wykonaj komendę Update-Database, aby zastosować migracje Entity Framework Core i utworzyć schemat bazy danych.
6. Kompilacja i uruchomienie:<br>
• Skompiluj projekt, wybierając konfigurację Debug w Visual Studio.<br>
• Uruchom aplikację, naciskając F5 lub klikając przycisk Start w Visual Studio.<br>
API będzie dostępne pod domyślnym adresem (np. https://localhost:5001 lub http://localhost:5000).
