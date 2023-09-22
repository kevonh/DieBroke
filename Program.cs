using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.CompilerServices;

namespace DieBroke;

internal class Program
{
    static FinancialConfig? financialConfig = new FinancialConfig();
    static TaxConfig? taxConfig = new TaxConfig();
    static string financialDataJson = "financialData.json";
    static string taxConfigJson = "taxConfig.json";
    static string outputPath = "FinancialSimulationResults.txt";


    static void Main(string[] args)
    {
        // Read data from file
        try
        {
            InitializeConfigFiles();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            return;
        }

        if (financialConfig is null || financialConfig.Scenarios is null)
        {
            Console.WriteLine("Failed to read financial data from file");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            return;
        }

        foreach (var scenario in financialConfig.Scenarios)
        {
            DateTime currentDate = DateTime.Now;
            try
            {
                // Run the simulation
                while (currentDate <= scenario.EndSimulationDate)
                {

                    UpdateCapitalChangeEvents(currentDate, scenario);
                    if (currentDate.Day == 1)
                        UpdateInvestments(currentDate, scenario);
                    //if it is the first day of the year, calculate tax owed
                    if (currentDate.DayOfYear == 1)
                    {
                        CalculateTaxesOwed(currentDate, scenario);
                        SaveLastYearAndReset(currentDate, scenario);
                    }

                    currentDate = currentDate.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                scenario.YearlyHistory.Add(new FinancialConfig.Scenario.FinancialState
                {
                    Year = currentDate.Year,
                    Income = 0,
                    CapitalGains = 0,
                    Expenses = 0,
                    UnadjustedTaxes = 0,
                    NetWorth = 0
                });
                Console.WriteLine(ex.Message);
            }
        }

        SaveResultsToFile(financialConfig);

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }
    public static void InitializeConfigFiles()
    {
        string financialConfigString = File.ReadAllText(financialDataJson);
        string taxConfigString = File.ReadAllText(taxConfigJson);

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        try
        {
            financialConfig = JsonConvert.DeserializeObject<FinancialConfig>(financialConfigString, settings);
            taxConfig = JsonConvert.DeserializeObject<TaxConfig>(taxConfigString, settings);
        }
        catch (JsonSerializationException jex)
        {
            var ex = jex.InnerException;
            while (ex?.InnerException != null)
                ex = ex.InnerException;

            throw new Exception(string.Format("Exception occurred when deserializing object {0}\n", ex?.Message), ex);
        }

        if (financialConfig is null)
        {
            throw new InvalidOperationException("Failed to deserialize the JSON into a FinancialConfig object.");
        }
        if (taxConfig is null)
        {
            throw new InvalidOperationException("Failed to deserialize the JSON into a TaxBracket object.");
        }
        Console.WriteLine($"[{GetLineNumber()}]Config Files are deserialized from JSON:");
        Console.WriteLine(financialConfig.ToString());
    }
    static void UpdateInvestments(DateTime currentDate, FinancialConfig.Scenario financialConfig)
    {
        if (financialConfig.InvestmentVehicles == null || !financialConfig.InvestmentVehicles.Any())
        {
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Updating Investments");
        Console.ResetColor();

        foreach (var investment in financialConfig.InvestmentVehicles)
        {
            if (investment.CurrentBalance <= 0)
            {
                continue;
            }
            // Calculate daily rate of return and update current balance
            decimal monthlyRateOfReturn = investment.RateOfReturn / 12;
            //calculate the monthly change in the investment
            var investmentChange = investment.CurrentBalance * monthlyRateOfReturn;
            //add the change to the investment balance
            investment.CurrentBalance += investmentChange;
            //log name, current balance, and change to the console
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - {investment.Name} - Balance: {investment.CurrentBalance:C} - Change: {investmentChange:C}");

            // Calculate the -Date to start transitioning to less risky investments
            DateTime startTransitionDate = financialConfig.EndSimulationDate.AddMonths(-investment.WaterfallToleranceMonths);

            // Check if we need to transition to less risky investments
            if (currentDate >= startTransitionDate)
            {
                //log to the console that a transition is happening
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transitioning {investment.Name} to less risky investments");
                // Calculate amount to transition out per month
                decimal monthlyTransitionAmount = investment.CurrentBalance / investment.TransitionPeriodMonths;
                // Find the next lower-risk investment vehicle, assuming FindLowerRiskInvestment is implemented
                var lowerRiskInvestment = FindLowerRiskInvestment(financialConfig.InvestmentVehicles, investment.RiskLevel);

                // If there's a lower-risk investment, transition into that
                if (lowerRiskInvestment != null)
                {
                    // if the transition amount is greater than the current balance, transition the entire balance
                    if (monthlyTransitionAmount > investment.CurrentBalance)
                    {
                        lowerRiskInvestment.CurrentBalance += investment.CurrentBalance;
                        investment.CurrentBalance = 0;
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transitioned {investment.Name} to {lowerRiskInvestment.Name} - Balance: {investment.CurrentBalance:C}");
                    }
                    // otherwise transition the calculated amount
                    else
                    {
                        lowerRiskInvestment.CurrentBalance += monthlyTransitionAmount;
                        investment.CurrentBalance -= monthlyTransitionAmount;
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transitioned {investment.Name} to {lowerRiskInvestment.Name} - Balance: {investment.CurrentBalance:C}");
                    }
                }
                //otherwise, if there's no lower risk investment, transition into cash buffer
                else
                {
                    // if the transition amount is greater than the current balance, transition the entire balance
                    if (monthlyTransitionAmount > investment.CurrentBalance)
                    {
                        financialConfig.CashBuffer += investment.CurrentBalance;
                        investment.CurrentBalance = 0;
                        //log that the transition happened and went from the current investment to the cash buffer
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transitioned {investment.Name} to Cash Buffer - Balance: {investment.CurrentBalance:C}");
                    }
                    // otherwise transition the calculated amount
                    else
                    {
                        financialConfig.CashBuffer += monthlyTransitionAmount;
                        investment.CurrentBalance -= monthlyTransitionAmount;
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transitioned {investment.Name} to Cash Buffer - Balance: {investment.CurrentBalance:C}");
                    }
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Done updating Investments");
        Console.ResetColor();

    }
    static void UpdateCapitalChangeEvents(DateTime currentDate, FinancialConfig.Scenario financialConfig)
    {
        if (financialConfig.CapitalChangeEvents == null || !financialConfig.CapitalChangeEvents.Any())
        {
            return;
        }

        // Get all active events
        var activeEvents = financialConfig.CapitalChangeEvents.Where(x => x.StartDate <= currentDate && x.EndDate >= currentDate).Where(x =>
            (x.Frequency == Frequency.Monthly && currentDate.Day == 1)
            || (x.Frequency == Frequency.Quarterly && currentDate.Day == 1 && currentDate.Month % 3 == 0)
            || (x.Frequency == Frequency.Yearly && currentDate.Day == 1 && currentDate.Month == 1)).ToList();

        if (!activeEvents.Any())
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Updating Capital Changes");
        Console.ResetColor();


        foreach (var eventItem in activeEvents)
        {
            eventItem.IsTriggered = false;

            //log to the console that the event has been triggered
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Triggered {eventItem.Name}");
            //log the cash buffer BEFORE the event is triggered
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer: {financialConfig.CashBuffer:C}");
            financialConfig.CashBuffer += eventItem.Amount;
            //log to console the event amount
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - {eventItem.Name} Amount: {eventItem.Amount:C}");
            //log to console the new cash buffer 
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer: {financialConfig.CashBuffer:C}");
            eventItem.IsTriggered = true;

            if (eventItem.Amount > 0)
            {
                financialConfig.CurrentYearIncome += eventItem.Amount;
                //log to console the amount of income
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Add To Annual IncomeIncome: {eventItem.Amount:C}");
                //log to console the current year income
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Current Year Income: {financialConfig.CurrentYearIncome:C}");
            }
            else
            {
                financialConfig.CurrentYearExpenses += eventItem.Amount;
                //log to console the amount of expenses
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Add to Annual Expense: {eventItem.Amount:C}");
                //log to console total annual expenses
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Current Year Expenses: {financialConfig.CurrentYearExpenses:C}");
            }


            // Adjust for inflation
            if (financialConfig.AdjustForInflation)
            {
                if (eventItem.Frequency == Frequency.Yearly)
                {
                    //calculate the amount to adjust
                    var adjustAmount = eventItem.Amount * financialConfig.InflationRate;
                    //add the amount to the event
                    eventItem.Amount += adjustAmount;
                    //log to console the inflation adjustment
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Adjusted {eventItem.Name} for inflation - Amount: {adjustAmount:C}");
                }
                else if (eventItem.Frequency == Frequency.Monthly)
                {
                    //calculate the amount to adjust
                    var adjustAmount = eventItem.Amount * financialConfig.InflationRate / 12;
                    //add the amount to the event
                    eventItem.Amount += adjustAmount;
                    //log to console the inflation adjustment
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Adjusted {eventItem.Name} for inflation - Amount: {adjustAmount:C}");
                }
                else if (eventItem.Frequency == Frequency.Quarterly)
                {
                    //calculate the amount to adjust
                    var adjustAmount = eventItem.Amount * financialConfig.InflationRate / 4;
                    //add the amount to the event
                    eventItem.Amount += adjustAmount;
                    //log to console the inflation adjustment
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Adjusted {eventItem.Name} for inflation - Amount: {adjustAmount:C}");
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported frequency: {eventItem.Frequency}");
                }
            }

        }

        CheckCashReserve(currentDate, financialConfig);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Done updating Capital Changes");
        Console.ResetColor();

    }

    private static void CheckCashReserve(DateTime currentDate, FinancialConfig.Scenario financialConfig)
    {
        // Check if CashBuffer is less than the minimum required
        if (financialConfig.CashBuffer < financialConfig.CashBufferMinimum)
        {
            //log to console that the cash buffer needs adjusting
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer needs adjusting, currently at {financialConfig.CashBuffer:C}");
            //calculate the target buffer amount
            decimal targetBuffer = (financialConfig.CashBufferMaximum + financialConfig.CashBufferMinimum) / 2;
            //log the target buffer amount
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Target Buffer: {targetBuffer:C}");
            //calculate the amount needed to bring the cash buffer to the target buffer
            decimal amountNeeded = targetBuffer - financialConfig.CashBuffer;
            //log to console the amount needed
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Amount needed: {amountNeeded:C}");

            while (amountNeeded > 0)
            {

                // Grab the first Investment Vehicle that has ANY money
                FinancialConfig.Scenario.InvestmentVehicle? highestRiskInvestment = financialConfig.InvestmentVehicles?.Where(iv => iv.CurrentBalance > 0)?.OrderByDescending(iv => iv.RiskLevel).FirstOrDefault();

                if (highestRiskInvestment is null)
                {
                    //log to console that all investments are empty
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - All investments are empty");
                    if (financialConfig.CashBuffer < amountNeeded)
                    {
                        //log to console that the user is out of money
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate} - User is out of money");
                        //exit back to the main routine and end the simulation
                        throw new Exception("User has run out of money");
                    }
                    else
                    {
                        financialConfig.CashBuffer -= amountNeeded;
                        //log to console the new balance of cash buffer and current -Date
                        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Paid {amountNeeded} from Cash Buffer with a new balance of {financialConfig.CashBuffer:C}");
                        amountNeeded = 0;
                        continue;
                    }
                }

                else if (highestRiskInvestment.CurrentBalance < amountNeeded)
                {
                    //log to console that the investment is empty and that we are transferring the entire balance, and note the current balance before transfer
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - {highestRiskInvestment.Name} is empty, transferring entire balance of {highestRiskInvestment.CurrentBalance:C}");
                    amountNeeded -= highestRiskInvestment.CurrentBalance;
                    financialConfig.CashBuffer += highestRiskInvestment.CurrentBalance;

                    //log to console the current -Date and that capital gains were triggered in the amount of the current balance
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Capital Gains triggered in the amount of {highestRiskInvestment.CurrentBalance:C}");
                    financialConfig.CurrentYearCapitalGains += highestRiskInvestment.CurrentBalance;
                    highestRiskInvestment.CurrentBalance = 0;
                }
                else
                {
                    //log to console that the investment is not empty and that we are transferring the amount needed, and note the current balance before transfer
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - {highestRiskInvestment.Name} is not empty, transferring {amountNeeded:C}");
                    highestRiskInvestment.CurrentBalance -= amountNeeded;
                    //log to console the current -Date and that capital gains were triggered in the amount of the current balance
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Capital Gains triggered in the amount of {amountNeeded:C}");
                    financialConfig.CurrentYearCapitalGains += amountNeeded;
                    financialConfig.CashBuffer += amountNeeded;
                    //log to console the current -Date and that the cash buffer was increased by the amount needed, and the current amount of the cash buffer
                    Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer increased by {amountNeeded:C} to {financialConfig.CashBuffer:C}");
                    amountNeeded = 0;
                }
            }
        }

        // check if we have too much cash in the buffer before end of each day
        if (financialConfig.CashBuffer > financialConfig.CashBufferMaximum)
        {
            //log to console that the cash buffer needs adjusting
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer needs adjusting.  Current balance = {financialConfig.CashBuffer:C}");

            // Calculate the amount needed to bring the CashBuffer to halfway between the minimum and maximum
            decimal targetBuffer = (financialConfig.CashBufferMaximum + financialConfig.CashBufferMinimum) / 2;
            //log to console the target buffer
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Target Buffer: {targetBuffer:C}");
            decimal amountToInvest = financialConfig.CashBuffer - targetBuffer;
            //log to console the amount to invest
            Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Amount to invest: {amountToInvest:C}");


            // Ensure investment search considers having money and having enough money
            FinancialConfig.Scenario.InvestmentVehicle? highestRiskInvestment = financialConfig.InvestmentVehicles?
                .Where(iv => iv.CurrentBalance > 0)?.OrderByDescending(iv => iv.RiskLevel).FirstOrDefault();

            if (highestRiskInvestment != null)
            {
                highestRiskInvestment.CurrentBalance += amountToInvest;
                financialConfig.CashBuffer -= amountToInvest;
                //log to the console that we're transferring money from the cash buffer to the investment. note the -Date and the investment account recieving funds, and the amount
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Transferring {amountToInvest:C} from Cash Buffer to {highestRiskInvestment.Name} with a new balance of {highestRiskInvestment.CurrentBalance:C}");
                //log to the console the new cash buffer amount and current -Date
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer: {financialConfig.CashBuffer:C}");
            }
        }
    }

    public static decimal AdjustedStandardDeduction(DateTime currentDate, FinancialConfig.Scenario financialConfig)
    {
        DateTime startDate = DateTime.Now; // replace with actual start date

        int yearsPassed = currentDate.Year - startDate.Year;
        decimal annualInflationRate = 0.04m; // replace with actual annual inflation rate

        return financialConfig.StandardDeduction * (decimal)Math.Pow((1 + (double)annualInflationRate), yearsPassed);
    }

    private static void CalculateTaxesOwed(DateTime currentDate, FinancialConfig.Scenario financialConfig)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Tax Time");
        Console.ResetColor();

        //apply the standard deduction to the year's income into a new variable we'll use to calculate taxes
        var taxableIncome = financialConfig.CurrentYearIncome - AdjustedStandardDeduction(currentDate, financialConfig);
        //log to console the current year income and the standard deduction
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Current Year Income: {financialConfig.CurrentYearIncome:C}");
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Standard Deduction: {financialConfig.StandardDeduction:C}");
        //if the taxable income is less than 0, set it to 0
        if (taxableIncome < 0)
            taxableIncome = 0;
        //log to console the taxable income
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Taxable Income: {taxableIncome:C}");


        //calculate taxes owed based on tax brackets and income
        //Get the Tax Brackets based on filing status
        TaxBracketType? bracket = taxConfig?.TaxBracketTypes?.Where(x => x.FilingStatus == financialConfig.TaxFilingStatus).FirstOrDefault();

        if (bracket is null)
            throw new Exception("No tax bracket found for filing status");

        if (bracket.TaxBrackets is null)
            throw new Exception("No taxable income brackets found for filing status");

        foreach (var taxBracket in bracket.TaxBrackets)
        {
            //if the current year income is greater than the current bracket upper limit, calculate taxes owed for this bracket
            if (taxableIncome > taxBracket.UpperBound)
            {
                var amountInBracket = taxBracket.UpperBound - taxBracket.LowerBound;
                var taxesOwedInBracket = amountInBracket * taxBracket.Rate;
                //log to console the tax bracket and taxes owed for this bracket
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Bracket:{taxBracket.Rate} - Owed: {taxesOwedInBracket:C}");
                financialConfig.CurrentYearTaxesOwed += taxesOwedInBracket;
            }
            //if the current year income is less than the current bracket lower limit, skip this bracket
            else if (taxableIncome < taxBracket.LowerBound)
            {
                //log to the console that no taxes were owed for this bracket
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Bracket:{taxBracket.Rate} - Owed: 0");
                continue;
            }
            //otherwise, calculate taxes owed for this bracket based on the amount earned within this bracket
            else
            {
                var amountInBracket = financialConfig.CurrentYearIncome - taxBracket.LowerBound;
                var taxesOwedInBracket = amountInBracket * taxBracket.Rate;
                financialConfig.CurrentYearTaxesOwed += taxesOwedInBracket;
                Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Bracket:{taxBracket.Rate} - Owed: {taxesOwedInBracket:C}");
            }
        }
        //add capital gains taxes owed
        financialConfig.CurrentYearTaxesOwed += financialConfig.CurrentYearCapitalGains * financialConfig.CapitalGainsTaxRate;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Capital Gains: {financialConfig.CurrentYearCapitalGains:C}");
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Capital Gains Owed: {(financialConfig.CurrentYearCapitalGains * financialConfig.CapitalGainsTaxRate):C}");

        //Pay taxes from the buffer, it's ok if it goes negative, because we'll sell investments to cover it
        financialConfig.CashBuffer -= financialConfig.CurrentYearTaxesOwed;
        //log to console the cash buffer before taxes
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer: {financialConfig.CashBuffer:C}");
        //log to console the taxes owed and the new cash buffer balance
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Taxes Owed: {financialConfig.CurrentYearTaxesOwed:C}");
        //pay capital gains from the buffer, it's OK if it goes negative, because we'll sell investments to cover it
        financialConfig.CashBuffer -= financialConfig.CurrentYearTaxesOwed;
        //log to console the new cash buffer balance
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Cash Buffer: {financialConfig.CashBuffer:C}");

        CheckCashReserve(currentDate, financialConfig);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{GetLineNumber()}]-Date: {currentDate.ToShortDateString()} - Fuck Tax Time!!");
        Console.ResetColor();
    }
    private static void SaveLastYearAndReset(DateTime currentYear, FinancialConfig.Scenario financialConfig)
    {
        //create a FinancialState object to add to the list of yearly history
        FinancialConfig.Scenario.FinancialState lastYear = new FinancialConfig.Scenario.FinancialState();
        //set the year to the previous year
        lastYear.Year = currentYear.Year - 1;
        //set the income to the current year income
        lastYear.Income = financialConfig.CurrentYearIncome;
        //set the capital gains to the current year capital gains
        lastYear.CapitalGains = financialConfig.CurrentYearCapitalGains;
        //set the expenses to the current year expenses
        lastYear.Expenses = financialConfig.CurrentYearExpenses;
        //set the taxes owed to the current year taxes owed
        lastYear.UnadjustedTaxes = financialConfig.CurrentYearTaxesOwed;
        //set the net worth to the current net worth
        lastYear.NetWorth = financialConfig.NetWorth;

        //log to the console the details of last year
        Console.WriteLine($"[{GetLineNumber()}]-Date: {lastYear.Year} - Income: {lastYear.Income:C} - Capital Gains: {lastYear.CapitalGains:C} - Expenses: {lastYear.Expenses:C} - Taxes: {lastYear.UnadjustedTaxes:C} - Net Worth: {lastYear.NetWorth:C}");

        //Add a new set of data to the List of FinancialState
        financialConfig.YearlyHistory.Add(lastYear);
        //reset the current year income, expenses, and taxes owed
        financialConfig.CurrentYearIncome = 0;
        financialConfig.CurrentYearExpenses = 0;
        financialConfig.CurrentYearTaxesOwed = 0;
        financialConfig.CurrentYearCapitalGains = 0;
    }
    static void SaveResultsToFile(FinancialConfig financialConfig)
    {
        File.Delete(outputPath);
        if (financialConfig.Scenarios is null)
            return;
        //TODO: Re-work this so it saves all scenarios to the same file
        foreach (var scenario in financialConfig.Scenarios)
        {
            File.AppendAllText(outputPath, scenario.ToString());
        }
    }

    public static FinancialConfig.Scenario.InvestmentVehicle? FindLowerRiskInvestment(List<FinancialConfig.Scenario.InvestmentVehicle> investments, int currentRiskLevel)
    {
        return investments.OrderByDescending(x => x.RiskLevel)
            .FirstOrDefault(x => x.RiskLevel < currentRiskLevel);
    }
    public static int GetLineNumber([CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
    {
        return line;
    }
}

