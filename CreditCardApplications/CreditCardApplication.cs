using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CreditCardApplications
{
    public class CreditCardApplication
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public decimal GrossAnnualIncome { get; set; }
        public string FrequentFlyerNumber { get; set; }
    }
}
