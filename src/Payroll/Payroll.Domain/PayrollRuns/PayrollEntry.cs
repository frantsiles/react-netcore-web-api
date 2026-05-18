using Api.Domain.Common;

namespace Payroll.Domain.PayrollRuns;

public class PayrollEntry : Entity
{
    public Guid EmployeeId { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string EmployeeName { get; private set; } = "";

    // Earnings
    public decimal BaseSalary { get; private set; }   // monthly contract salary
    public decimal GrossSalary { get; private set; }  // pro-rated for period
    public decimal OvertimePay { get; private set; }
    public decimal TotalGross => GrossSalary + OvertimePay;

    // Employee deductions (CR)
    public decimal CcssEmployee { get; private set; }  // 10.67%
    public decimal BancoPopular { get; private set; }  // 1.00%
    public decimal IncomeTax { get; private set; }     // progressive table
    public decimal TotalDeductions => CcssEmployee + BancoPopular + IncomeTax;
    public decimal NetPay => TotalGross - TotalDeductions;

    // Employer contributions (for cost reporting)
    public decimal CcssEmployer { get; private set; }       // 26.67%
    public decimal InsEmployer { get; private set; }        // 1.00%
    public decimal Fcl { get; private set; }                // 3.00%
    public decimal TotalEmployerContribution => CcssEmployer + InsEmployer + Fcl;
    public decimal TotalLaborCost => TotalGross + TotalEmployerContribution;

    private PayrollEntry() : base() { }

    public static PayrollEntry Create(
        Guid employeeId, string employeeNumber, string employeeName,
        decimal baseSalary, decimal grossSalary, decimal overtimePay,
        decimal ccssEmployee, decimal bancoPopular, decimal incomeTax,
        decimal ccssEmployer, decimal insEmployer, decimal fcl)
    {
        return new PayrollEntry
        {
            EmployeeId = employeeId,
            EmployeeNumber = employeeNumber,
            EmployeeName = employeeName,
            BaseSalary = baseSalary,
            GrossSalary = grossSalary,
            OvertimePay = overtimePay,
            CcssEmployee = ccssEmployee,
            BancoPopular = bancoPopular,
            IncomeTax = incomeTax,
            CcssEmployer = ccssEmployer,
            InsEmployer = insEmployer,
            Fcl = fcl,
        };
    }
}
