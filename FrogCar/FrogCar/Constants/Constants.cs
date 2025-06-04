namespace FrogCar.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class ErrorMessages
    {
        public const string Unauthorized = "Nie można zidentyfikować użytkownika.";
        public const string AdminOnly = "Brak uprawnień. Tylko administrator może wykonać tę operację.";
        public const string NotOwnerOrAdmin = "Brak uprawnień. Tylko właściciel ogłoszenia lub administrator może wykonać tę operację.";
        public const string InvalidModel = "Błąd walidacji.";
        public const string UsernameTaken = "Nazwa użytkownika jest już zajęta.";
        public const string EmailTaken = "Podany adres e-mail jest już używany.";
        public const string InvalidCredentials = "Nieprawidłowa nazwa użytkownika lub hasło.";
        public const string InvalidLoginData = "Nazwa użytkownika lub hasło nie mogą być puste.";
        public const string EmptyUsername = "Nowa nazwa użytkownika nie może być pusta.";
        public const string UserNotFound = "Użytkownik nie istnieje.";
        public const string InvalidToken = "Nieprawidłowy lub wygasły token resetujący.";
        public const string EmailConfigMissing = "Brakuje klucza EmailSecretKey w konfiguracji.";
        public const string FrontendUrlMissing = "Brakuje adresu ResetPasswordUrl w konfiguracji.";
        public const string ListingNotFound = "Ogłoszenie nie istnieje.";
        public const string BadRequestEmptyListing = "Ogłoszenie nie może być puste.";
        public const string BadRequestRequiredBrand = "Marka samochodu jest wymagana.";
        public const string BadRequestEngineCapacity = "Pojemność silnika musi być większa od 0.";
        public const string BadRequestSeats = "Liczba miejsc musi być większa od 0.";
        public const string BadRequestFuelType = "Typ paliwa jest wymagany.";
        public const string BadRequestCarType = "Typ samochodu jest wymagany.";
        public const string BadRequestInvalidFeature = "Każda cecha musi być wypełniona poprawnie.";
        public const string BadRequestRentalPrice = "Cena wynajmu na jeden dzień musi być większa niż 0.";
        public const string NoApprovedListingsForUser = "Brak zatwierdzonych ogłoszeń dla tego użytkownika.";
        public const string NoListingsAvailable = "Brak ogłoszeń.";
        public const string NoCarsInRegion = "Brak dostępnych samochodów w podanym regionie.";
        public const string NoNotifications = "Brak nowych powiadomień.";
        public const string BadRequestEmptyRental = "Dane wypożyczenia są wymagane.";
        public const string RentalEndDateBeforeStartDate = "Data zakończenia wypożyczenia musi być późniejsza niż data rozpoczęcia.";
        public const string CarNotAvailable = "Samochód jest już niedostępny.";
        public const string CarNotApproved = "Samochód nie jest zatwierdzony i nie może być wypożyczony.";
        public const string CannotRentOwnCar = "Nie możesz wypożyczyć własnego samochodu.";
        public const string CarAlreadyRentedInPeriod = "Samochód jest już wypożyczony w żądanym okresie.";
        public const string NoActiveRentalsForUser = "Brak aktywnych wypożyczeń dla tego użytkownika.";
        public const string NoRentalsFound = "Brak wypożyczeń w systemie.";
        public const string RentalNotFound = "Wypożyczenie nie istnieje.";
        public const string NotOwnerRenterOrAdmin = "Brak uprawnień. Tylko właściciel ogłoszenia, osoba wypożyczająca lub administrator może wyświetlić to wypożyczenie.";
        public const string NotOwnerOrAdminRentalStatus = "Brak uprawnień. Tylko właściciel ogłoszenia lub administrator może zmieniać status wypożyczenia.";
        public const string InvalidRentalStatus = "Nieprawidłowy status wypożyczenia.";
        public const string NoEndedRentalsForUser = "Brak zakończonych lub anulowanych wypożyczeń dla tego użytkownika.";
        public const string InvalidRating = "Ocena musi być w zakresie 1-5.";
        public const string ReviewForEndedRentalsOnly = "Recenzja może być wystawiona tylko dla zakończonych wypożyczeń.";
        public const string AlreadyReviewed = "Wystawiłeś już recenzję dla tego wypożyczenia.";
        public const string NoReviewsForListing = "Brak recenzji dla tego ogłoszenia.";
        public const string NotOwnerOrAdminRentalDeletion = "Nie masz uprawnień do usunięcia tego wypożyczenia.";
        public const string NoCarsMatchingCriteria = "Nie znaleziono samochodów pasujących do podanych kryteriów.";
        public const string RentalNotEnded = "Wypożyczenie sie nie zakończyło";
        public const string RentalNotFoundForUser = "Nie znaleziono wypożyczenia dla tego użytkownika";
        public const string CannotDeleteRentedCar = "Nie możesz usunąć wypożyczonego samochodu";
        public const string NotRenterOrAdminRentalDeletion = "Tylko najemca lub administrator może usunąć wypożyczenie.";
    }
}