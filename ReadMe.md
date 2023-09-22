This financial calculator is meant to allow users to input their current financial situation, which is presumed to be sufficient to retire, and validate the assumptions based on expected market returns on investments.

The calculator is not meant to be a substitute for professional financial advice. It is meant to be a tool to help you understand the assumptions that go into retirement planning.

There are two config files, financialdata.json and taxConfig.json. The financialdata.json file contains the financial data for the user, and the taxConfig.json file contains the tax information from the IRS 2021 tax brackets. 

The financialdata.json file is structured that you can include multiple different scenarios so that you can compare them without needing to re-run the program.

In the sample file, there are two scenarios provided to better understand how they work together.

The scenarios have the following properties:
	- Name: The name of the scenario
	- Birthdate: The birthdate of the user, this is used to calculate how much time is left before the end of the simulation
	- ExpectedLifespanYears: The expected lifespan of the user, this is used to calculate how much time is left before the end of the simulation
	- InflationRate: The expected inflation rate, this is used to adjust things during the course of the simulation
	- SpendingBuffer: This can be both a positive or negative number, but essentially it is used to indicate the starting point of the simulation, and is not used after that
	- SpendingBufferMinimum: The concept is that most people have some cash on hand, and prefer to keep that minimum at all time.  The program will take actions to ensure that there is always a minimum amount of Spending Buffer
	- SpendingBufferMaximum: If the spending buffer is above this amount, the program will take actions to reduce the spending buffer by investing money. This is used to ensure that the spending buffer does not get too large
	- TaxFilingStatus: This is used to determine the tax brackets to use for the simulation.  The options are:
		- Single
		- MarriedFilingJointly
	- StandardDeduction: The value provided is the standard deduction based on 2021.  If you itemize, and know what your normal deduction is, you can replace the value here with your own value
	- CapitalGainsTaxRate:  The capital gains tax rate to use for the simulation.  This is used to calculate the taxes on the investments.
	- InvestmentVehicles: This gives the user the flexibility to have multiple investment vehicles with different risk factors and different expected rates of return.
		- Name: The name of the investment vehicle
		- CurrentBalance: The current balance of the investment vehicle
		- RiskLevel: this helps the routine know which investment to invest into and which investment to liquidate from in the case more money is needed
			- The HIGHEST risk investment available is always used first
		- WaterfallToleranceMonths: The number of months from the end of the simulation that the investment vehicle will begin being liquidated
		- TransitionPeriodMonths: The number of months over which the investment vehicle will be liquidated, starting at the beginning of the transition
		- RateOfReturn: The expected rate of return for the investment vehicle.  
	- CapitalChangeEvents: This represents any known events that will cause a change in the capital of the user.  It can represent both positive and negative events.
		-- Examples: Salary, Bonus, Annuity, Pension, Social Security, etc.
		- Name: The name of the event
		- StartDate: The date the event starts, if no date is supplied, it is assumed to be the start of the simulation
		- EndDate: The date the event ends, if no date is supplied, it is assumed to be the end of the simulation
		- Amount: The amount of the event, this can be positive or negative
		- Frequency: The frequency of the event, this can be:
			- Monthly
			- Quarterly
			- Annually
			- OneTime
		- ChangeType: This is used to indicate whether the event is a positive or negative event. If type is not supplied, it will assume not-taxable income.  This can be:
			- RegularIncome
			- Expense




ToDo:

The concept of Leveraging is modeled as a negative return investment with some additional parameters
Leveraging should have an upper and lower tolerance bound, as well as a target
Lower bound indicates that it is time to leverage further
Upper bound indicates that it is time to pay back some debt
In theory, this works similar to a spending buffer.  You don't want it to go outside of the bounds
There should be a routine that checks percent leveraged quarterly for going lower than the threshold, indicating the need to leverage further
If available spending buffer goes lower than the spending buffer minimum, this would trigger a leverage event to replenish the spending buffer
Every leverage event should check for the upper leverage bound. If hit, this would trigger a sell event to reduce the leverage amount.

Things I'll need your input on:
Leverage upper, lower, and target percentages

We also talked about adding a couple of additional parameters to the InvestmentVehicle: UpperRateOfReturn, LowestRateOfReturn to give us our upper and lower boundary of tolerance.

For instance, if an investment vehicle hit a rate of return of X percent for any given period of time, you would consider transitioning to a different vehicle.  
Given that Cash has a negative rate equal to the inflation rate, I would assume that the rate X above would be between negative inflation and perhaps T-Bill rates?? 
I'm out of my wheelhouse on that, but you get the point.

I need to add more change types to handle tax deductible negative change events