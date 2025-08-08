# PensionPilot

PensionPilot is a tool for simulating retirement finances. It includes portfolio assets, study fund, pension fund, salary, company stocks (RSU). You can configure age of retirement, rate of returns for each source of income, rate of withdrawal, pension payouts, and tax payments (income tax, capital gains tax).

Built with GitHub Copilot & GPT-5. Might contain mistakes. The following description is the spec used to generate the tool.

## Goals

* Allow flexible **“what-if” inputs** for:

  * Retirement age
  * Investment return rates (by asset class)
  * Withdrawal rates before and after pension age
  * Tax assumptions
* Provide a **year-by-year projection** from current age until a configurable age
* Separate different income streams:

  * Portfolio (stocks, bonds, etc.)
  * Study fund (tax deductible fund available in some countries)
  * Pension fund
  * Salary (until retirement)
  * Company RSUs (vesting schedule + sale assumptions)
* Include **automatic tax calculations** for:

  * Income tax (progressive brackets)
  * Capital gains tax
* Output:

  * Final net worth at end of each year
  * Annual cashflow (pre- and post-tax)
  * Visual graphs: portfolio growth, cashflow vs expenses, asset depletion

---

## Input Parameters (Configurable Section)


### Personal & Timeline

| Parameter                   | Description                                      | Example |
| --------------------------- | ------------------------------------------------ | ------- |
| Current Age                 | Your age today                                   | 40      |
| Retirement Age              | When you stop working                            | 60      |
| Pension Age                 | When pension payments start                      | 67      |
| Life Expectancy (Model End) | Max modeled age                                  | 100     |

### Salary & RSUs

| Parameter              | Description                   | Example   |
| ---------------------- | ----------------------------- | --------- |
| Annual Gross Salary    | Before taxes                  | 480,000   |
| Annual Salary Growth % | Inflation-adjusted            | 2%        |
| RSU Total Grant Amount | Value of RSUs granted         | 200,000   |
| RSU Vesting Years      | How many years to fully vest  | 4         |
| RSU Sale Lag (Years)   | Delay from vesting to selling | 1         |

### Portfolio & Funds

| Parameter                   | Description                 | Example     |
| --------------------------- | --------------------------- | ----------- |
| Portfolio Current Value     | All taxable investments     | 2,000,000   |
| Portfolio Annual Return %   | Nominal return before taxes | 5%          |
| Portfolio Withdrawal Rate % | Before pension age          | 3.5%        |

| Parameter                        | Description                | Example   |
| -------------------------------- | -------------------------- | --------- |
| Study fund Value                 | Current study fund balance | 150,000 |
| Study fund Annual Return %       | Nominal return             | 4%        |
| Withdraw at Year X               | When it will be cashed out | Year 6    |

| Parameter               | Description                 | Example     |
| ----------------------- | --------------------------- | ----------- |
| Pension Fund Value      | Current balance             | 1,000,000   |
| Pension Annual Return % | Growth until pension starts | 4%          |
| Pension Monthly Payout  | After 67                    | 10,000      |

### Taxes

| Parameter           | Description                                | Example          |
| ------------------- | ------------------------------------------ | ---------------- |
| Income Tax Brackets | Table by annual income                     |                  |
| Capital Gains Tax % | On sale of assets                          | 25%              |
| RSU Tax Treatment   | Income tax at vesting, capital gains later | Custom rules     |

---

## Model Logic (Calculations)

1. **Before retirement age:**

   * Salary income → pay income tax → net savings → added to portfolio
   * RSUs vest & sell → taxed accordingly → added to portfolio
   * Portfolio grows at set rate
   * Study fund grows until withdrawal year

2. **After retirement age but before pension age:**

   * Withdraw from portfolio at set rate
   * Portfolio grows at return rate
   * Apply capital gains tax on withdrawals (or model net returns after tax)

3. **After pension age:**

   * Pension payouts → taxed as income
   * Withdraw from portfolio only if pension < expenses
   * Continue modeling portfolio returns & depletion

4. **Year-end net worth & cashflow summary**:

   * Portfolio value
   * Study fund (if still there)
   * Pension fund balance
   * Annual net cash available

---

## Outputs

* Table: Year | Age | Salary | RSUs | Pension | Portfolio Withdrawal | Taxes | Net Income | Ending Portfolio Value
* Charts:

  * Portfolio value over time
  * Annual income sources stacked over time
  * Year cashflow vs. expenses

---

## Extra Features

* Inflation adjustment for all amounts

---

## Tech Specs

* .NET 9
* ASP.NET Core Blazor WebAssembly
* Statically hostable
