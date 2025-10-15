using System;
using System.Collections.Generic;              // List<>, používaný při sběru validačních výsledků
using System.ComponentModel;                   // IDataErrorInfo (WPF validace přes binding)
using System.ComponentModel.DataAnnotations;   // DataAnnotations: [Required], [StringLength], Validator.TryValidateProperty
using System.Linq;                              // .First() nad kolekcí validačních výsledků
using System.Linq.Expressions;                  // (V tomto souboru se nepoužívá – lze odstranit)
using System.Runtime.CompilerServices;          // (Nepoužívá se – lze odstranit)
using System.Text;                              // (Nepoužívá se – lze odstranit)
using System.Threading.Tasks;                   // (Nepoužívá se – lze odstranit)

namespace DataEntity
{
    /// <summary>
    /// Abstraktní základ pro všechny entity v doméně.
    /// - Implementuje IDataErrorInfo => umožňuje WPF zobrazovat chyby ve vázaných ovládacích prvcích (Binding).
    /// - Obsahuje "konkurenční token" RowVersion (pro optimistickou konkurenci) a DatumVytvoreni (audit).
    /// - OnValidate(...) centralizuje vyhodnocení DataAnnotations nad jednotlivými vlastnostmi.
    /// </summary>
    public abstract class BaseModel : IDataErrorInfo
    {
        #region "validace"

        // IDataErrorInfo.Error je agregovaná chybová zpráva za celou entitu.
        // Autor se rozhodl ji NEPOUŽÍVAT a místo toho vyhazuje výjimku, aby bylo jasné,
        // že se má používat indexer IDataErrorInfo.this[propertyName].
        // Ve WPF se obvykle validační hlášky odebírají právě přes indexer pro konkrétní property.
        string IDataErrorInfo.Error
        {
            get
            {
                // Původně tu bývalo "return null;" (což by potichu říkalo „bez chyby“),
                // ale explicitně se hází NotSupportedException, aby se tato cesta nepoužívala omylem.
                throw new NotSupportedException(
                    "IDataErrorInfo.Error is not supported, use IDataErrorInfo.this[propertyName] instead.");
            }
        }

        // Indexer pro IDataErrorInfo – WPF ho volá pro každou vázanou vlastnost,
        // aby zjistilo, zda je konkrétní hodnota validní.
        // Vrací prázdný string = OK, nebo text chyby = nevalidní.
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                // Původně tu bývalo "return null;" (což by potvrdilo „bez chyby“),
                // ale správně se deleguje do OnValidate(propertyName), která provede DataAnnotations validaci.
                return OnValidate(propertyName);
            }
        }

        /// <summary>
        /// Vlastní validační metoda:
        /// - Ověří, že propertyName není prázdné.
        /// - Pomocí reflexe najde hodnotu vlastnosti.
        /// - Sestaví ValidationContext a zavolá Validator.TryValidateProperty(...)
        /// - Pokud se validace nepovede, vrátí první chybovou zprávu (pro WPF Binding).
        /// </summary>
        protected virtual string OnValidate(string propertyName)
        {
            // Ochrana proti chybě volajícího: propertyName musí být neprázdné.
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Invalid property name", propertyName);

            string error = string.Empty;

            // Reflexe: vezmi typ aktuální instance, najdi property jménem propertyName,
            // a získej její aktuální hodnotu (GetValue(this, null)).
            // POZN.: pokud by property neexistovala, GetProperty vrátí null a následné volání GetValue by spadlo.
            var value = this.GetType().GetProperty(propertyName).GetValue(this, null);

            // Připravíme si kolekci pro případné validační chyby (DataAnnotations může vrátit více chyb).
            var results = new List<ValidationResult>(1);

            // ValidationContext nese „kontext validace“ – objekt + jméno člena.
            // MemberName = propertyName říká Validatoru, kterou property právě validujeme.
            var context = new ValidationContext(this, null, null) { MemberName = propertyName };

            // TryValidateProperty provede validaci podle DataAnnotations atributů na dané property.
            // Např. [Required], [StringLength], [Range], [EmailAddress] apod.
            var result = Validator.TryValidateProperty(value, context, results);

            if (!result)
            {
                // Pokud validace neprošla, vezmeme první ValidationResult (typicky je stejně jen jeden)
                // a z něj vytáhneme ErrorMessage – to je text, který WPF ukáže u bound prvku.
                var validationResult = results.First();
                error = validationResult.ErrorMessage;
            }

            // Prázdný string => validní; neprázdný => chyba.
            return error;
        }

        #endregion

        /// <summary>
        /// RowVersion je „konkurenční token“ (optimistická konkurence).
        /// - [Timestamp] říká EF Core, že toto pole má být mapováno na SQL rowversion/timestamp.
        /// - Při UPDATE EF pošle původní RowVersion; pokud se v DB od posledního načtení změnila,
        ///   vyvolá se DbUpdateConcurrencyException (někdo mezitím záznam změnil).
        /// - Aplikace tak může upozornit uživatele na konflikt a nabídnout sloučení/obnovení.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// Datum a čas vytvoření záznamu.
        /// - Není speciálně řízeno EF; default se nastaví při vytvoření objektu v aplikaci.
        /// - Užitečné pro audit a řazení.
        /// - Pozor na časovou zónu a konzistenci (DateTime.Now vs. UTC).
        /// </summary>
        public DateTime DatumVytvoreni { get; set; } = DateTime.Now;
    }
}
