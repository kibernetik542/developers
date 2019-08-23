using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;


namespace ExchangeRateUpdater
{
    public class ExchangeRateProvider
    {
        /// <summary>
        /// Should return exchange rates among the specified currencies that are defined by the source. But only those defined
        /// by the source, do not return calculated exchange rates. E.g. if the source contains "EUR/USD" but not "USD/EUR",
        /// do not return exchange rate "USD/EUR" with value calculated as 1 / "EUR/USD". If the source does not provide
        /// some of the currencies, ignore them.
        /// </summary>
        public IEnumerable<ExchangeRate> GetExchangeRates(IEnumerable<Currency> currencies)
        {
            // Declarations
            var enumerable = currencies.ToList();
            var result = new List<ExchangeRate>();
            var response = GetData();
            var matchingRates = GetMatchingRates(response);

            if (!enumerable.Any())
            {
                return Enumerable.Empty<ExchangeRate>();
            }

            foreach (var item in matchingRates)
            {
                var currencyElement = item.Split('|');
                if (currencyElement.Length < 3) continue;
                var currency = currencyElement[1];
                if (enumerable.All(x => x.Code != currency)) continue;

                decimal.TryParse(currencyElement[0], out decimal source);
                decimal.TryParse(currencyElement[2], out decimal target);

                result.Add(new ExchangeRate(
                   new Currency(currencyElement[1]),
                   _crown,
                   target / source));
            }
            return result;
        }

        // URL for rates from Czech National Bank
        private const string CzechNationalBankRates =
            @"https://www.cnb.cz/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt";

        private readonly Currency _crown = new Currency("CZK");

        private string GetData()
        {
            using (var client = new HttpClient())
            {
                var responseResult = client.GetAsync(CzechNationalBankRates).Result;

                if (responseResult.IsSuccessStatusCode)
                {
                    return responseResult.Content.ReadAsStringAsync().Result;
                }
            }
            return string.Empty;
        }

        private IEnumerable<string> GetMatchingRates(string response)
        {
            var regex = new Regex(@"(\d+)(\d|.+)");

            var regexMatches = regex.Matches(response).Cast<Match>()
                .Select(x => x.Value);

            return regexMatches.Skip(1);
        }
    }
}