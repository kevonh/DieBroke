using System.Text;

namespace DieBroke;

public class FinancialConfig
{
    public List<Scenario>? Scenarios;
    public class Scenario
    {
        public string? Name { get; set; } // Name of the scenario
        public DateTime BirthDate { get; set; } = DateTime.MinValue; // The birth date of the person
        public int ExpectedLifespanYears { get; set; } // Expected lifespan in years
        public decimal CashBuffer { get; set; } = 0; // Initial cash available
        public decimal CashBufferMinimum { get; set; } // Minimum cash buffer to maintain
        public decimal CashBufferMaximum { get; set; } // Maximum cash buffer to maintain
        public decimal InflationRate { get; set; } // The annual inflation rate (e.g., 0.02 for 2%)
        public bool AdjustForInflation { get; set; } // Whether to adjust for inflation
        public decimal CurrentYearIncome { get; set; } // Current year income
        public decimal CurrentYearCapitalGains { get; set; } // Current year capital gains
        public decimal CurrentYearExpenses { get; set; } // Current year expenses
        public decimal CurrentYearTaxesOwed { get; set; } // Current year taxes owed
        public decimal CapitalGainsTaxRate { get; set; } // The capital gains tax rate (e.g., 0.18 for 18%)
        public string TaxFilingStatus { get; set; } = "MarriedFilingJointly"; // The tax filing status (e.g., "Single" or "MarriedFilingJointly")
        public decimal StandardDeduction { get; set; } // The standard deduction

        public List<InvestmentVehicle>? InvestmentVehicles { get; set; }
        public List<CapitalChangeEvent>? CapitalChangeEvents { get; set; }
        public List<TaxBracketType>? TaxBrackets { get; set; }
        public List<FinancialState> YearlyHistory { get; set; } = new List<FinancialState>();

        public decimal NetWorth
        {
            get
            {
                decimal netWorth = CashBuffer;

                if (InvestmentVehicles != null)
                {
                    netWorth += InvestmentVehicles.Sum(iv => iv.CurrentBalance);
                }

                return netWorth;
            }
        }
        public DateTime EndSimulationDate => BirthDate.AddYears(ExpectedLifespanYears);

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Name: {Name}");
            stringBuilder.AppendLine($"Year,Income,CapitalGains,Expenses,NetWorth,UnadjustedTaxes");
            foreach (var year in YearlyHistory)
            {
                if (year.NetWorth == 0)
                    stringBuilder.AppendLine($"Off to the Poor House with you!!  You're Broke!");
                else
                    //append a comma separated list of the year over year data
                    stringBuilder.AppendLine($"{year.Year},{year.Income:C},{year.CapitalGains:C},{year.Expenses:C},{year.NetWorth:C},{year.UnadjustedTaxes:C}");
            }

            return stringBuilder.ToString();
        }

        public class InvestmentVehicle
        {
            public string? Name { get; set; }
            public decimal CurrentBalance { get; set; } // Current balance in the investment
            public decimal RateOfReturn { get; set; } // Annual rate of return
            public int RiskLevel { get; set; } // A numerical value indicating the risk level
            public int WaterfallToleranceMonths { get; set; } // Number of months before death at which to transition this investment
            public int TransitionPeriodMonths { get; set; } // Number of months over which to transition the investment to the next lower-risk investment

            public override string ToString()
            {
                return $"{Name},{CurrentBalance:C}";
            }

        }

        public class CapitalChangeEvent
        {
            public string? Name { get; set; } // Name of the event (e.g., "Property Tax", "Car Payment")
            public decimal Amount { get; set; } // Amount of the event
            public Frequency Frequency { get; set; } // Frequency: OneTime, Monthly, Quarterly, Annually
            public DateTime StartDate { get; set; } = DateTime.MinValue;// Date when the event starts
            public DateTime EndDate { get; set; } = DateTime.MaxValue;// Date when the event ends
            public bool AdjustForInflation { get; set; } // Whether to adjust for inflation
            public bool IsTriggered { get; set; } // Whether the event has been triggered
            public override string ToString()
            {
                if (IsTriggered)
                    return $"{Name},{Amount:C}";
                else return "";
            }
        }

        //Class to track year over year data
        public class FinancialState
        {
            public int Year { get; set; }
            public decimal Income { get; set; }
            public decimal CapitalGains { get; set; }
            public decimal Expenses { get; set; }
            public decimal NetWorth { get; set; }
            public decimal UnadjustedTaxes { get; set; }
        }
    }
}

public enum Frequency
{
    OneTime,
    Monthly,
    Quarterly,
    Yearly
}

public enum ChangeType
{
    Expense,
    CapitalGains,
    RegularIncome
}