using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DieBroke
{
    public class TaxConfig
    {
        public List<TaxBracketType>? TaxBracketTypes { get; set; }
    }
    public class TaxBracketType
    {
        public string FilingStatus { get; set; } = "MarriedFilingJointly"; // Type of income (e.g., "Single" or "MarriedFilingJointly")
        public List<TaxBrackets>? TaxBrackets { get; set; } // List of taxable income brackets
    }

    public class TaxBrackets
    {
        public decimal LowerBound { get; set; } // Lower bound of the bracket
        public decimal UpperBound { get; set; } // Upper bound of the bracket
        public decimal Rate { get; set; } // Tax rate for the bracket
    }
}


