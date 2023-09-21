I. Introduction
DieBroke helps you to take control of your financial life and achieve financial independence. Use it to calculate a retirement schedule and budget so that you never run out of money.


II. How to Use DieBroke
Input Parameters
DieBroke accepts the following input parameters in a JSON file (see sample):
•	StartDate: The date of your next financial year
•	RetirementAge: Your planned retirement age
•	LifeExpectancy: Your estimated life expectancy
•	InflationRate: The estimated rate of inflation
•	CashBufferMinimum: The minimum amount of cash you want to keep in reserve
•	CashBufferMaximum: The maximum amount of cash you want to hold

•	CapitalChangeEvents: An array of cash capital change events:
    •	StartDate: The start date of the event
    •	EndDate: The end date of the event (optional, if One Time frequency is set)
    •	Amount: The amount of money being added or subtracted
    •	Frequency: The frequency of the event (one-time, monthly, quarterly or yearly)
    •	AdjustForInflation: A boolean indicating whether the amount should be adjusted for inflatio	n

•	InvestmentVehicles: An array of investment vehicles:
    •	Name: The name of the investment vehicle
    •	CurrentBalance: The current balance held in the vehicle
    •	ExpectedRateOfReturn: The expected rate of return for the investment vehicle
    •	RiskLevel: The risk level of the investment vehicle (on a scale of 1 to 10, where 1 is low risk and 10 is high risk)

The Frequency enum represents the frequency of a cash capital change event. It can take one of the following values:
public enum Frequency
{
    OneTime,
    Monthly,
    Quarterly,
    Yearly
}

If you need additional frequencies, please contact us at kevonh@gmail.com

Thank you for using DieBroke!