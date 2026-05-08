using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;
using Syncfusion.XlsIO.Implementation.Security;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;
        public PayrollRepository(OBMSDbContext oBMSDbContext)
        {
            _oBMSDbContext = oBMSDbContext;
        }

        #region Monthly Salary Advance
        public async Task<List<SalaryAdvance>> GetSalaryAdvanceById(int employeeId,int id)
        {
            var result = await _oBMSDbContext.SalaryAdvances.Where(sa => sa.EmployeeID == employeeId && sa.ID == id).ToListAsync();
            return new List<SalaryAdvance>(result);
        }
        public async Task<List<Employee>> GetEmployeeById(int employeeId)
        {
            var result = await _oBMSDbContext.Employees.Where(sa => sa.EMP_ID == employeeId).ToListAsync();
            return new List<Employee>(result);
        }
        public async Task<List<SalaryAdvance>> GetSalaryAdvanceByDateAndEmployee(SalaryAdvance salaryAdvance)
        {
            var result = await _oBMSDbContext.SalaryAdvances
                .Where(sa => sa.AdvanceDate == salaryAdvance.AdvanceDate &&
                              sa.EmployeeID == salaryAdvance.EmployeeID &&
                              sa.TransType == salaryAdvance.TransType &&
                              !sa.IsDeleted) // Assuming IsDeleted is a boolean property
                .ToListAsync();

            return result;
        }
        public async Task<List<SalaryAdvanceDto>> GetEmployeeList()
        {
            var result = (from employee in _oBMSDbContext.Employees
                          join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                          join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                          select new
                          {
                              employee.EMP_ID,
                              employee.EMP_NAME,
                              employee.EMP_IC_NEW,
                              employee.EMP_IC_OLD,
                              employee.EMP_PASSPORT_NO,
                              salaryDetails.EMPFL_BANK,
                              salaryDetails.EMPFL_BK_ACCNO,
                              salaryDetails.PAYMODE
                          }).ToList();

            var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
            {
                EMP_ID = x.EMP_ID,
                EMP_NAME = string.IsNullOrEmpty(x.EMP_NAME)
                            ? x.EMP_NAME
                            : x.EMP_NAME.Replace("''", "'"),
                EMP_IC_NEW = x.EMP_IC_NEW,
                EMP_IC_OLD = x.EMP_IC_OLD,
                EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
                EMPFL_BANK = x.EMPFL_BANK,
                EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
                PAYMODE = x.PAYMODE,
            }).ToList();

            return new List<SalaryAdvanceDto>(salaryAdvanceList);
        }

        //public async Task<List<SalaryAdvanceDto>> GetEmployeeListBySalaryAdvance(int TransType,string currentUser)
        //{
        //    bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

        //    var result = await (from employee in _oBMSDbContext.Employees
        //                        join ob in _oBMSDbContext.OBMSBranches on employee.EMP_BRANCH_CODE equals ob.BranchCode
        //                        join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
        //                        join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
        //                        ////join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into salaryAdvanceGroup
        //                        ////  from salaryAdvances in salaryAdvanceGroup.DefaultIfEmpty()
        //                        join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID
        //                        where salaryAdvance.TransType == TransType && salaryAdvance.IsDeleted == false 
        //                        && (isSuperAdmin || ob.Name == currentUser)
        //                        select new
        //                        {
        //                            salaryAdvance.ID,
        //                            employee.EMP_ID,
        //                            employee.EMP_NAME,
        //                            employee.EMP_IC_NEW,
        //                            employee.EMP_IC_OLD,
        //                            employee.EMP_PASSPORT_NO,
        //                            salaryDetails.EMPFL_BANK,
        //                            salaryDetails.EMPFL_BK_ACCNO,
        //                            salaryDetails.PAYMODE,
        //                            salaryAdvance.Amount,
        //                            salaryAdvance.Particulars
        //                        }).ToListAsync();

        //    var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
        //    {
        //        ID = x.ID,
        //        EMP_ID = x.EMP_ID,
        //        EMP_NAME = x.EMP_NAME,
        //        EMP_IC_NEW = x.EMP_IC_NEW,
        //        EMP_IC_OLD = x.EMP_IC_OLD,
        //        EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
        //        EMPFL_BANK = x.EMPFL_BANK,
        //        EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
        //        PAYMODE = x.PAYMODE,
        //        Amount = x.Amount,
        //        Particulars = x.Particulars,
        //    }).ToList();

        //    return new List<SalaryAdvanceDto>(salaryAdvanceList);
        //}

        public async Task<List<SalaryAdvanceDto>> GetEmployeeListBySalaryAdvance(int TransType, string currentUser)
        {
            bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var query = from salaryAdvance in _oBMSDbContext.SalaryAdvances
                        join employee in _oBMSDbContext.Employees
                            on salaryAdvance.EmployeeID equals employee.EMP_ID
                        join branch in _oBMSDbContext.OBMSBranches
                            on employee.EMP_BRANCH_CODE equals branch.BranchCode
                        join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                            on employee.EMP_CODE equals salaryDetails.EMPFL_CODE into salaryDetailsGroup
                        from sd in salaryDetailsGroup.DefaultIfEmpty()
                        join employment in _oBMSDbContext.EmploymentDetails
                            on employee.EMP_CODE equals employment.EMPPAY_CODE into employmentGroup
                        from emp in employmentGroup.DefaultIfEmpty()
                        where salaryAdvance.TransType == TransType
                              && salaryAdvance.IsDeleted == false
                              && (isSuperAdmin || branch.Name == currentUser)
                        select new
                        {
                            salaryAdvance.ID,
                            employee.EMP_ID,
                            employee.EMP_NAME,
                            employee.EMP_ROLE,
                            employee.EMP_IC_NEW,
                            employee.EMP_IC_OLD,
                            employee.EMP_PASSPORT_NO,
                            EMPFL_BANK = sd != null ? sd.EMPFL_BANK : null,
                            EMPFL_BK_ACCNO = sd != null ? sd.EMPFL_BK_ACCNO : null,
                            PAYMODE = sd != null ? sd.PAYMODE : null,
                            salaryAdvance.Amount,
                            salaryAdvance.Particulars,
                            salaryAdvance.AdvanceDate
                        };

            // Ensure distinct SalaryAdvance IDs to prevent duplicates
            var result = await query
                .GroupBy(x => x.ID)
                .Select(g => g.First())
                .ToListAsync();

            var salaryAdvanceList = result.Select(x => new SalaryAdvanceDto
            {
                ID = x.ID,
                EMP_ID = x.EMP_ID,
                EMP_NAME = string.IsNullOrEmpty(x.EMP_NAME)
                            ? x.EMP_NAME
                            : x.EMP_NAME.Replace("''", "'"),
                EMP_ROLE = x.EMP_ROLE,
                EMP_IC_NEW = x.EMP_IC_NEW,
                EMP_IC_OLD = x.EMP_IC_OLD,
                EMP_PASSPORT_NO = x.EMP_PASSPORT_NO,
                EMPFL_BANK = x.EMPFL_BANK,
                EMPFL_BK_ACCNO = x.EMPFL_BK_ACCNO,
                PAYMODE = x.PAYMODE,
                Amount = x.Amount,
                Particulars = x.Particulars,
                AdvanceDate = x.AdvanceDate
            }).ToList();

            return salaryAdvanceList;
        }

        public async Task<List<SalaryAdvanceDto>> GetEmployeeListByBranchCode(string branchCode)
        {
            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where employee.EMP_BRANCH_CODE == branchCode
                                select new SalaryAdvanceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = employee.EMP_NAME,
                                    EMP_CODE = employee.EMP_CODE,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : ""
                                })
                    .GroupBy(dto => dto.EMP_ID) // Group by employee ID
                    .Select(group => group.First()) // Select the first element of each group
                    .ToListAsync();


            return new List<SalaryAdvanceDto>(result);
        }
        public async Task<List<SalaryAdvanceDto>> GetEmployeeListByAdvanceID(int Id)
        {
            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where salaryAdvance.ID == Id
                                select new SalaryAdvanceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = employee.EMP_NAME,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : ""
                                }).ToListAsync();

            return new List<SalaryAdvanceDto>(result);
        }
        public async Task<SalaryAdvance> SaveAndUpdateSalaryMonthlyAdvance(SalaryAdvance salaryAdvance)
        {
            var existingRecord = await _oBMSDbContext.SalaryAdvances
                .FirstOrDefaultAsync(s => s.ID == salaryAdvance.ID);

            if (existingRecord != null)
            {
                // Update properties
                existingRecord.EmployeeID = salaryAdvance.EmployeeID;
                existingRecord.AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                existingRecord.AdvanceDate = salaryAdvance.AdvanceDate;
                existingRecord.VoucherNo = salaryAdvance.VoucherNo;
                existingRecord.Amount = salaryAdvance.Amount;
                existingRecord.NoOfInstallments = salaryAdvance.NoOfInstallments;
                existingRecord.PaymentType = salaryAdvance.PaymentType;
                existingRecord.Particulars = salaryAdvance.Particulars;
                existingRecord.TransType = salaryAdvance.TransType;
                existingRecord.IsDeleted = salaryAdvance.IsDeleted;
                existingRecord.LastUpdate = DateTime.Now;
                existingRecord.LastUpdatedBy = salaryAdvance.LastUpdatedBy;
                _oBMSDbContext.Update(existingRecord);
                await _oBMSDbContext.SaveChangesAsync();
                return existingRecord;
            }
            else
            {
                _oBMSDbContext.Add(salaryAdvance);
                await _oBMSDbContext.SaveChangesAsync();
                return salaryAdvance;
            }


        }
        #endregion
        public async Task<List<InventoryCategory>> GetInventoryCategories()
        {
            var query = _oBMSDbContext.InventoryCategories
                .OrderBy(ic => ic.Name)
                .Select(ic => new InventoryCategory
                {
                    ID = ic.ID,
                    Name = $"{ic.Name}({(ic.Cat == "P" ? "Purchase" : "Expenses")})",
                    Cat = ic.Cat,
                    AssetType = ic.AssetType
                });

            return await query.ToListAsync();
        }
        public string GetNewVoucherNumberAsync(int transType)
        {
            var result = _oBMSDbContext.SalaryAdvances
                .Where(s => s.TransType == transType)
                .OrderByDescending(s => s.LastUpdate) // Ensure latest record comes first
                .AsEnumerable()
                .Select(s =>
                    int.TryParse(s.VoucherNo, out var voucherNo) ? voucherNo : (int?)null) // Parse VoucherNo
                .Where(v => v.HasValue) // Exclude null values
                .Max() ?? 0; // Get the highest valid VoucherNo or default to 0

            var newVoucherNo = result + 1;

            return newVoucherNo.ToString("000000");
        }
        public List<ItemMasterDto> GetUniformItemRows(int AdvanceID, int Category)
        {
            var query = from itemMaster in _oBMSDbContext.ItemMasters
                        join employeeItemIssue in _oBMSDbContext.EmployeeItemIssues
                            on new { ItemID = itemMaster.ID, AdvanceID }
                            equals new { ItemID = employeeItemIssue.ItemID, AdvanceID = employeeItemIssue.AdvanceID }
                            into itemIssueGroup
                        from employeeItemIssue in itemIssueGroup.DefaultIfEmpty()
                        where itemMaster.CategoryID == Category
                        select new ItemMasterDto
                        {
                            ID = employeeItemIssue != null ? employeeItemIssue.ID : 0,
                            AdvanceID = employeeItemIssue != null ? employeeItemIssue.AdvanceID : 0,
                            ItemID = itemMaster.ID,
                            Name = itemMaster.Name,
                            Price = itemMaster.SellPrice ?? 0,
                            Quantity = employeeItemIssue != null ? employeeItemIssue.Quantity: 0
                            
                        };

            return query.OrderBy(x=> x.Name).ToList();
        }
        public bool GetSalaryProcessDateByEmployeeID(int employeeID, int year, int month)
        {
            var payslipExists = _oBMSDbContext.PaySlips
                .Where(p => p.EmployeeID == employeeID && p.Period.Year == year && p.Period.Month == month)
                .OrderByDescending(p => p.Period)
                .Take(1)
                .Any();

            return payslipExists;

        }
        public DateTime GetResignDateByEmployeeID(int employeeID)
        {
            var resignDate = _oBMSDbContext.Employees
                .Join(
                    _oBMSDbContext.EmploymentDetails,
                    employee => employee.EMP_CODE,
                    employmentDetails => employmentDetails.EMPPAY_CODE,
                    (employee, employmentDetails) => new { employee, employmentDetails }
                )
                .Where(joinResult => !joinResult.employee.HasTransfered && joinResult.employee.EMP_ID == employeeID)
                .Select(joinResult => joinResult.employmentDetails.EMPPAY_DATE_RESIGNED)
                .FirstOrDefault();
            return resignDate ?? new DateTime(1900, 1, 1);

            //return (DateTime)(resignDate != default(DateTime) ? resignDate : new DateTime(1900, 1, 1));

        }
        #region MiscTransaction Region
        public async Task<IEnumerable<MiscTrans>> GetMiscTrans(string currentUser)
        {
            bool isSuperAdmin = currentUser.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

            var query = from m in _oBMSDbContext.MiscTrans
                        join e in _oBMSDbContext.Employees on m.EmployeeID equals e.EMP_ID
                        join ob in _oBMSDbContext.OBMSBranches on e.EMP_BRANCH_CODE equals ob.BranchCode
                        where isSuperAdmin || m.LastUpdatedBy == currentUser
                        select m;

            return await query.Distinct().ToListAsync();
        }
        public async Task<List<MiscTrans>> GetMiscTransById(int id)
        {
            var result = await _oBMSDbContext.MiscTrans.Where(m => m.ID == id).ToListAsync();
            return new List<MiscTrans>(result);
        }
        public async Task<MiscTrans> SaveAndUpdateMiscTrans(MiscTrans miscTrans)
        {
            var existingMiscTrans = await _oBMSDbContext.MiscTrans.FirstOrDefaultAsync(mt => mt.ID == miscTrans.ID);
            if (existingMiscTrans != null)
            {
                existingMiscTrans.TransDate = miscTrans.TransDate;
                existingMiscTrans.EmployeeID = miscTrans.EmployeeID;
                existingMiscTrans.TransType = miscTrans.TransType;
                existingMiscTrans.Amount = miscTrans.Amount;
                existingMiscTrans.Particulars = miscTrans.Particulars;
                existingMiscTrans.LastUpdate = DateTime.Now;
                existingMiscTrans.LastUpdatedBy = miscTrans.LastUpdatedBy;
                _oBMSDbContext.Update(existingMiscTrans);
                await _oBMSDbContext.SaveChangesAsync();
                return existingMiscTrans;
            }
            else
            {
                miscTrans.LastUpdate = DateTime.Now;
                _oBMSDbContext.MiscTrans.Add(miscTrans);
                await _oBMSDbContext.SaveChangesAsync();
                return miscTrans;
            }

        }

        public async Task DeleteMiscTransById(int id)
        {
            var miscTrans = await _oBMSDbContext.MiscTrans.FindAsync(id);
            _oBMSDbContext.MiscTrans.Remove(miscTrans);
            await _oBMSDbContext.SaveChangesAsync();
        }

        #endregion
        public async Task<List<SalaryAdvance>> SaveAndUpdateSalaryDailyAdvances(List<SalaryAdvance> salaryAdvances)
        {
            List<SalaryAdvance> updatedRecords = new List<SalaryAdvance>();

            foreach (var salaryAdvance in salaryAdvances)
            {
                var existingRecord = await _oBMSDbContext.SalaryAdvances
                    .FirstOrDefaultAsync(s => s.ID == salaryAdvance.ID);

                if (existingRecord != null)
                {
                    // Update properties
                    existingRecord.EmployeeID = salaryAdvance.EmployeeID;
                    existingRecord.AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                    existingRecord.AdvanceDate = salaryAdvance.AdvanceDate;
                    existingRecord.VoucherNo = salaryAdvance.VoucherNo;
                    existingRecord.Amount = salaryAdvance.Amount;
                    existingRecord.NoOfInstallments = salaryAdvance.NoOfInstallments;
                    existingRecord.PaymentType = salaryAdvance.PaymentType;
                    existingRecord.Particulars = salaryAdvance.Particulars;
                    existingRecord.TransType = salaryAdvance.TransType;
                    existingRecord.IsDeleted = salaryAdvance.IsDeleted;
                    existingRecord.LastUpdate = DateTime.Now;
                    existingRecord.LastUpdatedBy = salaryAdvance.LastUpdatedBy;

                    _oBMSDbContext.Update(existingRecord);
                    updatedRecords.Add(existingRecord);
                }
                else
                {
                    _oBMSDbContext.Add(salaryAdvance);
                    updatedRecords.Add(salaryAdvance);
                }
            }

            await _oBMSDbContext.SaveChangesAsync();
            return updatedRecords;
        }

        public List<EmployeeDailyAdvanceRow> GetDailyAdvanceList(DateTime advanceDate, int employeeID, int advanceType)
        {
            if (employeeID > 0)
            {

                int noOfDays = DateTime.DaysInMonth(advanceDate.Year, advanceDate.Month);
                List<EmployeeDailyAdvanceRow> employeeDailyAdvanceRowList = new List<EmployeeDailyAdvanceRow>();
                int startDay = 1;

                //var employee = _oBMSDbContext.Employees.Where(e => e.EMP_ID == employeeID).SingleOrDefault().EMP_CODE;
                var emp = _oBMSDbContext.Employees.SingleOrDefault(e => e.EMP_ID == employeeID);
                if (emp == null) return new List<EmployeeDailyAdvanceRow>();

                var employee = emp.EMP_CODE;

                if (employee != null)
                {
                    var employment = Get(employee);
                    if (employment.EMPPAY_DATE_RESIGNED?.Year != 1)
                    {
                        if (advanceDate > employment.EMPPAY_DATE_RESIGNED)
                        {
                            startDay = 0;
                            //noOfDays = 0;
                        }
                        else if ((advanceDate.Month == employment.EMPPAY_DATE_RESIGNED?.Month) && (advanceDate.Year == employment.EMPPAY_DATE_RESIGNED?.Year))
                        {
                            noOfDays = (int)(employment.EMPPAY_DATE_RESIGNED?.Day);
                        }
                    }

                    if ((advanceDate.Month == employment.EMPPAY_DATE_JOINED.Month) && (advanceDate.Year == employment.EMPPAY_DATE_JOINED.Year))
                    {
                        startDay = employment.EMPPAY_DATE_JOINED.Day;
                    }

                    for (int i = startDay; i <= noOfDays; i++)
                    {
                        employeeDailyAdvanceRowList.Add(new EmployeeDailyAdvanceRow(0, 0, i, 0, "", 0, "", "", 0, false, "", DateTime.MinValue, DateTime.MinValue));
                    }

                    var salaryAdvances = _oBMSDbContext.SalaryAdvances
                        .Where(sa => sa.TransType == advanceType &&
                                     sa.AdvanceDate.Month == advanceDate.Month &&
                                     sa.AdvanceDate.Year == advanceDate.Year &&
                                     sa.EmployeeID == employeeID &&
                                     !sa.IsDeleted)
                        .ToList();

                    foreach (var salaryAdvance in salaryAdvances)
                    {
                        //int dayIndex = salaryAdvance.AdvanceDate.Day - 1; // Subtract 1 to convert day number to zero-based index
                        int dayIndex = salaryAdvance.AdvanceDate.Day - startDay;
                        if (dayIndex >= 0 && dayIndex < employeeDailyAdvanceRowList.Count)
                        {
                            employeeDailyAdvanceRowList[dayIndex].ID = salaryAdvance.ID;
                            employeeDailyAdvanceRowList[dayIndex].Day = salaryAdvance.AdvanceDate.Day;
                            employeeDailyAdvanceRowList[dayIndex].Amount = salaryAdvance.Amount;
                            employeeDailyAdvanceRowList[dayIndex].EmployeeID = salaryAdvance.EmployeeID;
                            employeeDailyAdvanceRowList[dayIndex].TransType = salaryAdvance.TransType;
                            employeeDailyAdvanceRowList[dayIndex].Particulars = salaryAdvance.Particulars;
                            employeeDailyAdvanceRowList[dayIndex].IsDeleted = salaryAdvance.IsDeleted;
                            employeeDailyAdvanceRowList[dayIndex].LastUpdatedBy = salaryAdvance.LastUpdatedBy;
                            employeeDailyAdvanceRowList[dayIndex].VoucherNo = salaryAdvance.VoucherNo;
                            employeeDailyAdvanceRowList[dayIndex].PaymentType = salaryAdvance.PaymentType;
                            employeeDailyAdvanceRowList[dayIndex].NoOfInstallments = salaryAdvance.NoOfInstallments;
                            employeeDailyAdvanceRowList[dayIndex].AdvanceDate = salaryAdvance.AdvanceDate;
                            employeeDailyAdvanceRowList[dayIndex].AdvanceTakenDate = salaryAdvance.AdvanceTakenDate;
                        }
                    }
                    return employeeDailyAdvanceRowList;
                }
            }

            return new List<EmployeeDailyAdvanceRow>(); // Return empty list if employee is not found

        }

        public EmploymentDetails Get(string employeeNo)
        {
            var employmentDetails = _oBMSDbContext.EmploymentDetails.FirstOrDefault(ed => ed.EMPPAY_CODE == employeeNo);

            if (employmentDetails != null)
            {
                return employmentDetails;
            }
            return employmentDetails;
        }
        public async Task<string?> GetEmployeeNoAsync(int employeeId)
        {
            var employee = await _oBMSDbContext.Employees
                .Where(e => e.EMP_ID == employeeId)
                .Select(e => e.EMP_CODE)
                .FirstOrDefaultAsync();

            return employee;
        }

        public async Task<IEnumerable<SalaryAdvance>> GetSalaryAdvancesAsync(DateTime advanceDate, int employeeId, int transType)
        {
            return await _oBMSDbContext.SalaryAdvances
                .Where(sa => sa.AdvanceDate == advanceDate.Date &&
                             sa.EmployeeID == employeeId &&
                             sa.TransType == transType &&
                             !sa.IsDeleted)
                .ToListAsync();
        }
        public async Task<bool> DeleteSalaryAdvanceAsync(int salaryAdvanceID, string currentUser)
        {
            try
            {
                var salaryAdvance = await _oBMSDbContext.SalaryAdvances.FirstOrDefaultAsync(x => x.ID == salaryAdvanceID && !x.IsDeleted);
                if (salaryAdvance == null)
                    return false;

                salaryAdvance.IsDeleted = true;
                salaryAdvance.LastUpdatedBy = currentUser;
                salaryAdvance.LastUpdate = DateTime.Now;

                await _oBMSDbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                throw;
            }
        }       

        public async Task<SalaryAdvance?> GetEmployeeLoanIdAsync(int id, int transType)
        {
            return await _oBMSDbContext.SalaryAdvances
                             .Where(sa => sa.ID == id && sa.TransType == transType && sa.IsDeleted == false)
                             .FirstOrDefaultAsync();
        }
        #region Attendance

        public ActionResult<IEnumerable<ClientMaster>> GetClients(DateTime period, string branchCode)
        {
            IQueryable<ClientMaster> query = _oBMSDbContext.ClientMasters;

            if (!string.IsNullOrEmpty(branchCode))
            {
                query = query.Where(c => c.Branch == branchCode);
            }

            query = query.Where(c => c.SuperClientCode != null);
            query = query.Where(c => c.Status == "Active");

            //if (period != null)
            //{
            //    query = query.Where(c => _oBMSDbContext.Agreements.Any(a => a.Client == c.Code && a.AgreementDate <= period) &&
            //                              !_oBMSDbContext.TerminatedAgreements.Any(ta => ta.Client == c.Code && ta.TerminationDate <= period));
            //}
            query = query.OrderBy(c => c.Shortname);
            List<ClientMaster> clients = query.ToList();
            return clients;
        }
        public Attendance AttendanceByEmployeeID(DateTime Period, int employeeID)
        {
            try
            {
                var attendance = _oBMSDbContext.Attendances
                    .Where(e => e.EmployeeID == employeeID && e.Period == Period)
                    .FirstOrDefault();

                if (attendance != null)
                {
                    return attendance;
                }
                return attendance;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<AttendanceDetails>> AttendanceDetailsByID(int Id)
        {
            var result = _oBMSDbContext.AttendanceDetails
                .Where(e => e.AttendanceID == Id).ToList();

            return new List<AttendanceDetails>(result);


        }
        public async Task<List<AttendanceDetails>> GetAttendanceDetailsList(int AttendanceID)
        {
            try
            {
                var attendancedetailsfactoryList = (
                    from ad in _oBMSDbContext.AttendanceDetails
                    join a in _oBMSDbContext.Attendances on ad.AttendanceID equals a.ID
                    join c1 in _oBMSDbContext.ClientMasters on new { a.Branch, Code = ad.Client } equals new { c1.Branch, c1.Code } into clientJoin
                    from c1 in clientJoin.DefaultIfEmpty()
                    join c2 in _oBMSDbContext.ClientMasters on new { a.Branch, Code = ad.OTClient } equals new { c2.Branch, c2.Code } into otClientJoin
                    from c2 in otClientJoin.DefaultIfEmpty()
                    where ad.AttendanceID == AttendanceID
                    select new AttendanceDetails
                    {
                        ID = ad.ID,
                        AttendanceID = ad.AttendanceID,
                        AttendanceDate = ad.AttendanceDate,
                        Client = ad.Client ?? "",
                        TimeStart = ad.TimeStart ?? DateTime.MinValue,
                        TimeEnd = ad.TimeEnd ?? DateTime.MinValue,
                        OTClient = ad.OTClient ?? "",
                        OTTimeStart = ad.OTTimeStart ?? DateTime.MinValue,
                        OTTimeEnd = ad.OTTimeEnd ?? DateTime.MinValue,
                        Type = ad.Type,
                        LastUpdate = ad.LastUpdate
                    }).ToList();

                return attendancedetailsfactoryList;

            }
            catch
            {
                throw;
            }
        }

        public async Task<List<SalaryAttendenceDto>> GetEmployeeDetails(string branchCode, string employeeNo)
        {
            //var employee = _oBMSDbContext.Employees
            //    .Where(e => e.EMP_CODE == employeeNo && e.EMP_BRANCH_CODE == branchCode)
            //    .FirstOrDefault();
            //return employee;

            var result = await (from employee in _oBMSDbContext.Employees
                                join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                                join employeement in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employeement.EMPPAY_CODE
                                join salarystructure in _oBMSDbContext.SalaryStructures on employeement.SALARYLAB equals salarystructure.SalaryId
                                join salaryAdvance in _oBMSDbContext.SalaryAdvances on employee.EMP_ID equals salaryAdvance.EmployeeID into advancesGroup
                                from salaryAdvance in advancesGroup.DefaultIfEmpty()
                                where employee.EMP_CODE == employeeNo && employee.EMP_BRANCH_CODE == branchCode
                                select new SalaryAttendenceDto
                                {
                                    ID = (salaryAdvance != null) ? salaryAdvance.ID : 0,
                                    EMP_ID = employee.EMP_ID,
                                    EMP_NAME = string.IsNullOrEmpty(employee.EMP_NAME)
                                                ? employee.EMP_NAME
                                                : employee.EMP_NAME.Replace("''", "'"),
                                    EMP_CODE = employee.EMP_CODE,
                                    EMP_IC_NEW = employee.EMP_IC_NEW,
                                    EMP_IC_OLD = employee.EMP_IC_OLD,
                                    EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                                    EMP_DATE_OF_BIRTH = employee.EMP_DATE_OF_BIRTH,
                                    SalaryStructure = employee.NewSalaryStructure.ToString(),
                                    EMPFL_BANK = salaryDetails.EMPFL_BANK,
                                    EMPFL_BK_ACCNO = salaryDetails.EMPFL_BK_ACCNO,
                                    EPFDETECT = salaryDetails.EPFDETECT,
                                    SOCSODETECT = salaryDetails.SOCSODETECT,
                                    INCOMETAXDETECT = salaryDetails.INCOMETAXDETECT,
                                    EMPFL_EPFNO = salaryDetails.EMPFL_EPFNO,
                                    PAYMODE = salaryDetails.PAYMODE,
                                    Amount = (salaryAdvance != null) ? salaryAdvance.Amount : 0,
                                    Particulars = (salaryAdvance != null) ? salaryAdvance.Particulars : "",
                                    EMPPAY_DATE_JOINED = employeement.EMPPAY_DATE_JOINED,
                                    EMPPAY_DATE_RESIGNED = employeement.EMPPAY_DATE_RESIGNED,
                                    ATTENDANCEALLOWANCE = employeement.ATTENDANCEALLOWANCE,
                                    SpecialAllowance = employeement.SpecialAllowance,
                                    EMPPAY_BASIC_RATE = employeement.EMPPAY_BASIC_RATE,
                                    Name = salarystructure.Name,
                                }).ToListAsync();

            return new List<SalaryAttendenceDto>(result);


        }
        public int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            int num;

            if (DateTime.Now.Month >= birthDate.Month)
            {
                DateTime now = DateTime.Now;

                if (now.Month == birthDate.Month)
                {
                    now = DateTime.Now;
                    num = now.Day >= birthDate.Day ? 1 : 0;
                }
                else
                {
                    num = 1;
                }
            }
            else
            {
                num = 0;
            }

            if (num == 0)
            {
                --age;
            }

            return age;
        }
        public int GetAnnualLeave(int employeeID, DateTime period)
        {
            try
            {
                var leaveQuery = from attendanceDetail in _oBMSDbContext.AttendanceDetails
                                 join attendance in _oBMSDbContext.Attendances on attendanceDetail.AttendanceID equals attendance.ID
                                 join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
                                 join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
                                 where attendanceDetail.AttendanceDate < period && attendanceDetail.Type == 8 && employee.EMP_ID == employeeID
                                 select new
                                 {
                                     LeaveTaken = 1, // Assuming 1 for LeaveTaken
                                     LeaveAvailable = 0
                                 };

                var leaveAvailableQuery = from leaveSystem in _oBMSDbContext.LeaveSystems
                                          join employee in _oBMSDbContext.Employees on leaveSystem.LS_ID equals employee.EMP_ID
                                          join employmentDetail in _oBMSDbContext.EmploymentDetails on employee.EMP_CODE equals employmentDetail.EMPPAY_CODE
                                          where employee.EMP_ID == employeeID
                                          select new
                                          {
                                              LeaveTaken = 0,
                                              LeaveAvailable = leaveSystem.al0to1 * ((period.Year - employmentDetail.EMPPAY_DATE_JOINED.Year == 0) ? DateDiffInMonths(employmentDetail.EMPPAY_DATE_JOINED, period) / 12 : 0)
                                              // Add similar conditions for other cases
                                          };

                //var result = leaveQuery.Union(leaveAvailableQuery).GroupBy(x => 1)
                //                          .Select(g => new
                //                          {
                //                              LeaveTaken = g.Sum(x => x.LeaveTaken),
                //                              LeaveAvailable = g.Sum(x => x.LeaveAvailable)
                //                          })
                //                          .FirstOrDefault();

                var result = new
                {
                    LeaveTaken = leaveQuery.Sum(x => x.LeaveTaken),
                    LeaveAvailable = leaveAvailableQuery.Sum(x => x.LeaveAvailable)
                };

                //return (int)result.LeaveAvailable - result.LeaveTaken;

                return result != null ? ((int)result.LeaveAvailable - result.LeaveTaken) : 0;

            }
            catch
            {
                throw;
            }
        }

        public int DateDiffInMonths(DateTime startDate, DateTime endDate)
        {
            return (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
        }

        //public async Task<ActionResult> SaveAndUpdateAttendance(Attendance attendanceModel, List<AttendanceDetails> attendanceDetails)
        //{
        //    try
        //    {
        //        if (attendanceModel != null)
        //        {
        //            var existingattendance = _oBMSDbContext.Attendances.Where(a => a.ID == attendanceModel.ID).SingleOrDefault();
        //            if (existingattendance != null)
        //            {
        //                existingattendance.EmployeeID = attendanceModel.EmployeeID;
        //                existingattendance.Period = attendanceModel.Period;
        //                existingattendance.Branch = attendanceModel.Branch;
        //                existingattendance.Shift2Type = attendanceModel.Shift2Type;
        //                existingattendance.Shift2Rate = attendanceModel.Shift2Rate;
        //                existingattendance.Bonus = attendanceModel.Bonus;
        //                existingattendance.KPIDeduction = attendanceModel.KPIDeduction;
        //                existingattendance.AllowanceDeduction = attendanceModel.AllowanceDeduction;
        //                existingattendance.SpecialAllowanceDeduction = attendanceModel.SpecialAllowanceDeduction;
        //                existingattendance.LastUpdate = attendanceModel.LastUpdate;
        //                existingattendance.LastUpdatedBy = attendanceModel.LastUpdatedBy;

        //                _oBMSDbContext.Update(existingattendance);
        //                await _oBMSDbContext.SaveChangesAsync();

        //                List<AttendanceDetails> updatedRecords = new List<AttendanceDetails>();
        //                foreach (var attendanceDetail in attendanceDetails)
        //                {
        //                    var existingRecord = await _oBMSDbContext.AttendanceDetails
        //                        .FirstOrDefaultAsync(s => s.ID == attendanceDetail.ID);

        //                    if (existingRecord != null)
        //                    {
        //                        // Update properties
        //                        existingRecord.AttendanceID = attendanceDetail.AttendanceID;
        //                        existingRecord.AttendanceDate = attendanceDetail.AttendanceDate;
        //                        existingRecord.Client = attendanceDetail.Client;
        //                        existingRecord.TimeStart = attendanceDetail.TimeStart;
        //                        existingRecord.TimeEnd = attendanceDetail.TimeEnd;
        //                        existingRecord.OTClient = attendanceDetail.OTClient;
        //                        existingRecord.OTTimeStart = attendanceDetail.OTTimeStart;
        //                        existingRecord.OTTimeEnd = attendanceDetail.OTTimeEnd;
        //                        existingRecord.Type = attendanceDetail.Type;
        //                        existingRecord.LastUpdate = DateTime.Now;
        //                        existingRecord.LastUpdatedBy = attendanceDetail.LastUpdatedBy;

        //                        _oBMSDbContext.Update(existingRecord);
        //                        updatedRecords.Add(existingRecord);
        //                    }
        //                    else
        //                    {
        //                        attendanceDetail.AttendanceID = existingattendance.ID;
        //                        _oBMSDbContext.Add(attendanceDetail);
        //                        updatedRecords.Add(attendanceDetail);
        //                    }
        //                }
        //                await _oBMSDbContext.SaveChangesAsync();
        //            }
        //            else
        //            {
        //                _oBMSDbContext.Add(attendanceModel);
        //                await _oBMSDbContext.SaveChangesAsync();

        //                List<AttendanceDetails> updatedRecords = new List<AttendanceDetails>();
        //                foreach (var attendanceDetail in attendanceDetails)
        //                {
        //                    var existingRecord = await _oBMSDbContext.AttendanceDetails
        //                        .FirstOrDefaultAsync(s => s.ID == attendanceDetail.ID);

        //                    if (existingRecord != null)
        //                    {
        //                        // Update properties
        //                        existingRecord.AttendanceID = attendanceDetail.AttendanceID;
        //                        existingRecord.AttendanceDate = attendanceDetail.AttendanceDate;
        //                        existingRecord.Client = attendanceDetail.Client;
        //                        existingRecord.TimeStart = attendanceDetail.TimeStart;
        //                        existingRecord.TimeEnd = attendanceDetail.TimeEnd;
        //                        existingRecord.OTClient = attendanceDetail.OTClient;
        //                        existingRecord.OTTimeStart = attendanceDetail.OTTimeStart;
        //                        existingRecord.OTTimeEnd = attendanceDetail.OTTimeEnd;
        //                        existingRecord.Type = attendanceDetail.Type;
        //                        existingRecord.LastUpdate = DateTime.Now;
        //                        existingRecord.LastUpdatedBy = attendanceDetail.LastUpdatedBy;

        //                        _oBMSDbContext.Update(existingRecord);
        //                        updatedRecords.Add(existingRecord);
        //                    }
        //                    else
        //                    {
        //                        attendanceDetail.AttendanceID = attendanceModel.ID;
        //                        _oBMSDbContext.Add(attendanceDetail);
        //                        updatedRecords.Add(attendanceDetail);
        //                    }
        //                }
        //                await _oBMSDbContext.SaveChangesAsync();
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }

        //    return null;
        //}

        public async Task<ActionResult> SaveAndUpdateAttendance(Attendance attendanceModel, List<AttendanceDetails> attendanceDetails)
        {
            try
            {
                if (attendanceModel != null)
                {
                    // Check if Attendance already exists
                    var existingattendance = await _oBMSDbContext.Attendances
                        .Where(a => a.ID == attendanceModel.ID)
                        .SingleOrDefaultAsync();

                    if (existingattendance != null)
                    {
                        // Update existing attendance
                        existingattendance.EmployeeID = attendanceModel.EmployeeID;
                        existingattendance.Period = attendanceModel.Period;
                        existingattendance.Branch = attendanceModel.Branch;
                        existingattendance.Shift2Type = attendanceModel.Shift2Type;
                        existingattendance.Shift2Rate = attendanceModel.Shift2Rate;
                        existingattendance.Bonus = attendanceModel.Bonus;
                        existingattendance.KPIDeduction = attendanceModel.KPIDeduction;
                        existingattendance.AllowanceDeduction = attendanceModel.AllowanceDeduction;
                        existingattendance.SpecialAllowanceDeduction = attendanceModel.SpecialAllowanceDeduction;
                        existingattendance.LastUpdate = attendanceModel.LastUpdate;
                        existingattendance.LastUpdatedBy = attendanceModel.LastUpdatedBy;

                        _oBMSDbContext.Update(existingattendance);
                        await _oBMSDbContext.SaveChangesAsync();

                        // --- AttendanceDetails logic ---
                        var existingRecords = await _oBMSDbContext.AttendanceDetails
                            .Where(ad => ad.AttendanceID == existingattendance.ID)
                            .ToListAsync();

                        foreach (var attendanceDetail in attendanceDetails)
                        {
                            var existingRecord = existingRecords
                                .FirstOrDefault(ad => ad.AttendanceDate.Date == attendanceDetail.AttendanceDate.Date);

                            if (existingRecord != null)
                            {
                                // Update existing
                                existingRecord.Client = attendanceDetail.Client;
                                existingRecord.TimeStart = attendanceDetail.TimeStart;
                                existingRecord.TimeEnd = attendanceDetail.TimeEnd;
                                existingRecord.OTClient = attendanceDetail.OTClient;
                                existingRecord.OTTimeStart = attendanceDetail.OTTimeStart;
                                existingRecord.OTTimeEnd = attendanceDetail.OTTimeEnd;
                                existingRecord.Type = attendanceDetail.Type;
                                existingRecord.LastUpdate = DateTime.Now;
                                existingRecord.LastUpdatedBy = attendanceDetail.LastUpdatedBy;

                                _oBMSDbContext.Update(existingRecord);
                            }
                            else
                            {
                                // Insert new WITHOUT setting ID (Identity handled by DB)
                                var newRecord = new AttendanceDetails
                                {
                                    AttendanceID = existingattendance.ID,
                                    AttendanceDate = attendanceDetail.AttendanceDate,
                                    Client = attendanceDetail.Client,
                                    TimeStart = attendanceDetail.TimeStart,
                                    TimeEnd = attendanceDetail.TimeEnd,
                                    OTClient = attendanceDetail.OTClient,
                                    OTTimeStart = attendanceDetail.OTTimeStart,
                                    OTTimeEnd = attendanceDetail.OTTimeEnd,
                                    Type = attendanceDetail.Type,
                                    LastUpdate = DateTime.Now,
                                    LastUpdatedBy = attendanceDetail.LastUpdatedBy
                                };

                                _oBMSDbContext.Add(newRecord);
                            }
                        }

                        await _oBMSDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        // Add new Attendance
                        _oBMSDbContext.Add(attendanceModel);
                        await _oBMSDbContext.SaveChangesAsync();

                        // --- AttendanceDetails logic for new attendance ---
                        foreach (var attendanceDetail in attendanceDetails)
                        {
                            var newRecord = new AttendanceDetails
                            {
                                AttendanceID = attendanceModel.ID,
                                AttendanceDate = attendanceDetail.AttendanceDate,
                                Client = attendanceDetail.Client,
                                TimeStart = attendanceDetail.TimeStart,
                                TimeEnd = attendanceDetail.TimeEnd,
                                OTClient = attendanceDetail.OTClient,
                                OTTimeStart = attendanceDetail.OTTimeStart,
                                OTTimeEnd = attendanceDetail.OTTimeEnd,
                                Type = attendanceDetail.Type,
                                LastUpdate = DateTime.Now,
                                LastUpdatedBy = attendanceDetail.LastUpdatedBy
                            };

                            _oBMSDbContext.Add(newRecord);
                        }

                        await _oBMSDbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return null;
        }



        public async Task<bool> DeleteAttendanceAsync(int dID)
        {
            try
            {
                // Fetch attendance details and delete
                var attendanceDetails = await _oBMSDbContext.AttendanceDetails
                    .Where(ad => ad.AttendanceID == dID)
                    .ToListAsync();

                if (attendanceDetails.Any())
                {
                    _oBMSDbContext.AttendanceDetails.RemoveRange(attendanceDetails);
                }

                // Fetch attendance and delete
                var attendance = await _oBMSDbContext.Attendances
                    .FirstOrDefaultAsync(a => a.ID == dID);

                if (attendance != null)
                {
                    // Extract employeeId and period from attendance
                    var employeeId = attendance.EmployeeID; // Replace with actual property name
                    var period = attendance.Period;         // Replace with actual property name

                    _oBMSDbContext.Attendances.Remove(attendance);

                    // Delete from AdvanceRepayment
                    var paySlipId = await _oBMSDbContext.PaySlips
                        .Where(p => p.EmployeeID == employeeId && p.Period == period)
                        .Select(p => p.ID)
                        .FirstOrDefaultAsync();

                    if (paySlipId != 0)
                    {
                        _oBMSDbContext.AdvanceRepayments.RemoveRange(
                            _oBMSDbContext.AdvanceRepayments.Where(ar => ar.PaySlipID == paySlipId)
                        );
                    }

                    // Delete from PaySlip
                    _oBMSDbContext.PaySlips.RemoveRange(
                        _oBMSDbContext.PaySlips.Where(p => p.EmployeeID == employeeId && p.Period == period)
                    );
                }

                // Save all changes in a single transaction
                await _oBMSDbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                throw; // Properly log exceptions in production code
            }
        }

        public List<EmployeeDto> GetList(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, DateTime attendancePeriod, string status)
        {
            var query = (from emp in _oBMSDbContext.Employees
                         join empDetails in _oBMSDbContext.EmploymentDetails on emp.EMP_CODE equals empDetails.EMPPAY_CODE
                         join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails on emp.EMP_CODE equals salaryDetails.EMPFL_CODE
                         join attendance in _oBMSDbContext.Attendances on emp.EMP_ID equals attendance.EmployeeID
                         join attendanceDetails in _oBMSDbContext.AttendanceDetails on attendance.ID equals attendanceDetails.AttendanceID
                         where emp.HasTransfered == false
                         select new EmployeeDto
                         {
                             EMP_ID = emp.EMP_ID,
                             EMP_ROLE = emp.EMP_ROLE,
                             EMP_CODE = emp.EMP_CODE,
                             EMP_NAME = string.IsNullOrEmpty(emp.EMP_NAME)
                                        ? emp.EMP_NAME
                                        : emp.EMP_NAME.Replace("''", "'"),
                             EMP_CLIENT = emp.EMP_CLIENT,
                             EMP_ADDRESS1 = emp.EMP_ADDRESS1,
                             EMP_ADDRESS2 = emp.EMP_ADDRESS2,
                             EMP_POST_CODE = emp.EMP_POST_CODE,
                             EMP_TOWN = emp.EMP_TOWN,
                             EMP_STATE = emp.EMP_STATE,
                             EMP_CITIZEN = emp.EMP_CITIZEN,
                             EMP_CHECKLIST = emp.EMP_CHECKLIST,
                             EMP_NATIONAL = emp.EMP_NATIONAL,
                             EMP_PHONE = emp.EMP_PHONE,
                             EMP_MOBILEPHONE = emp.EMP_MOBILEPHONE,
                             EMP_HGH_EDU = emp.EMP_HGH_EDU,
                             EMP_DATE_OF_BIRTH = emp.EMP_DATE_OF_BIRTH,
                             EMP_IC_OLD = emp.EMP_IC_OLD,
                             EMP_IC_NEW = emp.EMP_IC_NEW,
                             EMP_IC_COLOR = emp.EMP_IC_COLOR,
                             EMP_PASSPORT_NO = emp.EMP_PASSPORT_NO,
                             EMP_SEX = emp.EMP_SEX,
                             EMP_RACE = emp.EMP_RACE,
                             EMP_MARTIAL_STATUS = emp.EMP_MARTIAL_STATUS,
                             EMP_SPOUSE_NAME = emp.EMP_SPOUSE_NAME,
                             EMP_SP_IC = emp.EMP_SP_IC,
                             EMP_NO_CHILD = emp.EMP_NO_CHILD,
                             EMP_SP_WORK = emp.EMP_SP_WORK,
                             EMP_PER_NAME_CONTACT = emp.EMP_PER_NAME_CONTACT,
                             EMP_CONTACT_ADDRESS1 = emp.EMP_CONTACT_ADDRESS1,
                             EMP_CONTACT_ADDRESS2 = emp.EMP_CONTACT_ADDRESS2,
                             EMP_CONTACT_POST_CODE = emp.EMP_CONTACT_POST_CODE,
                             EMP_CONTACT_TOWN = emp.EMP_CONTACT_TOWN,
                             EMP_CONTACT_STATE = emp.EMP_CONTACT_STATE,
                             EMP_CONTACT_TELEPHONE = emp.EMP_CONTACT_TELEPHONE,
                             EMP_BRANCH_CODE = emp.EMP_BRANCH_CODE,
                             OldBranch = emp.OldBranch,
                             TransferDate = emp.TransferDate,
                             LASTUPDATE = emp.LASTUPDATE,
                             NewSalaryStructure = emp.NewSalaryStructure,
                             SalaryStructure1000_3h = emp.SalaryStructure1000_3h,
                             EMPPAY_DATE_RESIGNED = empDetails.EMPPAY_DATE_RESIGNED,
                             EMPPAY_DATE_JOINED = empDetails.EMPPAY_DATE_JOINED,
                             TMPGUARD = salaryDetails.TMPGUARD,
                             Period = attendance.Period

                         }).Distinct();

            // Apply filters based on parameters
            if (!string.IsNullOrEmpty(branch))
                query = query.Where(emp => emp.EMP_BRANCH_CODE == branch);

            if (!string.IsNullOrEmpty(employeeType))
            {
                if (employeeType.Contains("TEMPORARY"))
                    query = query.Where(emp => emp.TMPGUARD == false);
                if (employeeType == "TEMPORARYSTAFF")
                    query = query.Where(emp => emp.EMP_ROLE.Contains("STAFF"));
                else if (employeeType == "TEMPORARYGUARD")
                    query = query.Where(emp => emp.EMP_ROLE.Contains("GUARD"));
                else
                    query = query.Where(emp => emp.EMP_ROLE.Contains(employeeType));
            }

            if (resignedDate.Year != 1)
                //query = query.Where(empDetails => (empDetails.EMPPAY_DATE_RESIGNED ?? new DateTime(2100, 1, 1)) >= resignedDate);
                if (joinDate.Year != 1)
                    query = query.Where(emp => emp.EMPPAY_DATE_JOINED <= joinDate);
            if (attendancePeriod.Year != 1)
                query = query.Where(emp => emp.Period.Year == attendancePeriod.Year && emp.Period.Month == attendancePeriod.Month);

            query = query.OrderBy(emp => emp.EMP_NAME);

            return query.ToList();
        }
        //public List<EmployeeDto> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status)
        //{
        //    var defaultResignedDate = new DateTime(2100, 1, 1);

        //    var query = from e in _oBMSDbContext.Employees
        //                     join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
        //                     join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
        //                     where
        //                           (e.HasTransfered == false ||
        //                            (e.HasTransfered == true && e.TransferDate >= resignedDate))
        //                     select new EmployeeDto
        //                     {
        //                         EMP_ID = e.EMP_ID,
        //                         EMP_ROLE = e.EMP_ROLE,
        //                         EMP_CODE = e.EMP_CODE,
        //                         EMP_NAME = e.EMP_NAME,
        //                         EMP_CLIENT = e.EMP_CLIENT,
        //                         EMP_ADDRESS1 = e.EMP_ADDRESS1,
        //                         EMP_ADDRESS2 = e.EMP_ADDRESS2,
        //                         EMP_POST_CODE = e.EMP_POST_CODE,
        //                         EMP_TOWN = e.EMP_TOWN,
        //                         EMP_STATE = e.EMP_STATE,
        //                         EMP_NATIONAL = e.EMP_NATIONAL,
        //                         EMP_PHONE = e.EMP_PHONE,
        //                         EMP_MOBILEPHONE = e.EMP_MOBILEPHONE,
        //                         EMP_CITIZEN = e.EMP_CITIZEN,
        //                         EMP_CHECKLIST = e.EMP_CHECKLIST,
        //                         EMP_HGH_EDU = e.EMP_HGH_EDU,
        //                         EM_WORK_EXP = e.EM_WORK_EXP,
        //                         EMP_DATE_OF_BIRTH = e.EMP_DATE_OF_BIRTH,
        //                         EMP_IC_OLD = e.EMP_IC_OLD,
        //                         EMP_IC_NEW = e.EMP_IC_NEW,
        //                         EMP_IC_COLOR = e.EMP_IC_COLOR,
        //                         EMP_PASSPORT_NO = e.EMP_PASSPORT_NO,
        //                         EMP_SEX = e.EMP_SEX,
        //                         EMP_RACE = e.EMP_RACE,
        //                         EMP_MARTIAL_STATUS = e.EMP_MARTIAL_STATUS,
        //                         EMP_SPOUSE_NAME = e.EMP_SPOUSE_NAME,
        //                         EMP_SP_IC = e.EMP_SP_IC,
        //                         EMP_NO_CHILD = e.EMP_NO_CHILD,
        //                         EMP_SP_WORK = e.EMP_SP_WORK,
        //                         EMP_PER_NAME_CONTACT = e.EMP_PER_NAME_CONTACT,
        //                         EMP_CONTACT_ADDRESS1 = e.EMP_CONTACT_ADDRESS1,
        //                         EMP_CONTACT_ADDRESS2 = e.EMP_CONTACT_ADDRESS2,
        //                         EMP_CONTACT_POST_CODE = e.EMP_CONTACT_POST_CODE,
        //                         EMP_CONTACT_TOWN = e.EMP_CONTACT_TOWN,
        //                         EMP_CONTACT_STATE = e.EMP_CONTACT_STATE,
        //                         EMP_CONTACT_TELEPHONE = e.EMP_CONTACT_TELEPHONE,
        //                         EMP_BRANCH_CODE = e.EMP_BRANCH_CODE,
        //                         OldBranch = e.OldBranch,
        //                         TransferDate = e.TransferDate,
        //                         LASTUPDATE = e.LASTUPDATE,
        //                         NewSalaryStructure = e.NewSalaryStructure,
        //                         SalaryStructure1000_3h = e.SalaryStructure1000_3h
        //                     };
        //    // Apply filters based on parameters
        //    //if (!string.IsNullOrEmpty(status))
        //    //{
        //    //    if (status == "Active")
        //    //    {
        //    //        if (resignedDate.Month == 12)
        //    //        {
        //    //            query = query.Where(ed =>
        //    //                // Case for Active status with ResignedDate in December
        //    //                ed.EMPPAY_DATE_RESIGNED == null ||
        //    //                (ed.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
        //    //                 ed.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1)
        //    //            );
        //    //        }
        //    //        else
        //    //        {
        //    //            query = query.Where(ed =>
        //    //                // Case for Active status with ResignedDate not in December
        //    //                ed.EMPPAY_DATE_RESIGNED == null ||
        //    //                (ed.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
        //    //                 ed.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1)
        //    //            );
        //    //        }
        //    //    }
        //    //    else if (status == "Inactive")
        //    //    {
        //    //        // Case for Inactive status where EMPPAY_DATE_RESIGNED is not null
        //    //        query = query.Where(ed => ed.EMPPAY_DATE_RESIGNED != null);
        //    //    }
        //    //}

        //    if (!string.IsNullOrEmpty(employeeType))
        //        query = query.Where(e => e.EMP_ROLE.Contains(employeeType));

        //    if (!string.IsNullOrEmpty(branch))
        //        query = query.Where(e => e.EMP_BRANCH_CODE == branch);

        //    if (resignedDate.Year != 1)
        //        query = query.Where(ed =>
        //                (ed.EMPPAY_DATE_RESIGNED != null && ed.EMPPAY_DATE_RESIGNED >= resignedDate) ||
        //                defaultResignedDate >= resignedDate);

        //    if (joinDate.Year != 1)
        //        query = query.Where(ed => ed.EMPPAY_DATE_JOINED <= joinDate);

        //    query = query.OrderBy(emp => emp.EMP_NAME);

        //    return query.ToList();
        //}

        public List<EmployeeDto> getListByEmployee(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status)
        {
            var defaultResignedDate = new DateTime(2100, 1, 1);
            var result = new List<EmployeeDto>();
            // Base query
            if (joinDate.Year != 1)
            {
                var query = from e in _oBMSDbContext.Employees
                            join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
                            where (e.HasTransfered == false ||
                                  (e.HasTransfered == true && e.TransferDate >= resignedDate))
                            select new
                            {
                                Employee = e,
                                EmploymentDetail = ed
                            };

                // Apply filters before projection

                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Active")
                    {
                        if (resignedDate.Month == 12)
                        {
                            query = query.Where(q =>
                           q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                           (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                        else
                        {
                            query = query.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                    }
                    else if (status == "Inactive")
                    {
                        query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                    }
                }

                if (!string.IsNullOrEmpty(employeeType))
                    query = query.Where(q => q.Employee.EMP_ROLE == employeeType);

                if (!string.IsNullOrEmpty(branch))
                    query = query.Where(q => q.Employee.EMP_BRANCH_CODE == branch);

                if (resignedDate.Year != 1)
                    query = query.Where(q =>
                        q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null || q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

                if (joinDate.Year != 1)
                    query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

                query = query.OrderBy(emp => emp.Employee.EMP_NAME);

                // Project the results into EmployeeDto
                result = query
                    .OrderBy(q => q.Employee.EMP_NAME)
                    .Select(q => new EmployeeDto
                    {
                        EMP_ID = q.Employee.EMP_ID,
                        EMP_ROLE = q.Employee.EMP_ROLE,
                        EMP_CODE = q.Employee.EMP_CODE,
                        EMP_NAME = string.IsNullOrEmpty(q.Employee.EMP_NAME)
                            ? q.Employee.EMP_NAME
                            : q.Employee.EMP_NAME.Replace("''", "'"),
                        EMP_CLIENT = q.Employee.EMP_CLIENT,
                        EMP_ADDRESS1 = q.Employee.EMP_ADDRESS1,
                        EMP_ADDRESS2 = q.Employee.EMP_ADDRESS2,
                        EMP_POST_CODE = q.Employee.EMP_POST_CODE,
                        EMP_TOWN = q.Employee.EMP_TOWN,
                        EMP_STATE = q.Employee.EMP_STATE,
                        EMP_NATIONAL = q.Employee.EMP_NATIONAL,
                        EMP_PHONE = q.Employee.EMP_PHONE,
                        EMP_MOBILEPHONE = q.Employee.EMP_MOBILEPHONE,
                        EMP_CITIZEN = q.Employee.EMP_CITIZEN,
                        EMP_CHECKLIST = q.Employee.EMP_CHECKLIST,
                        EMP_HGH_EDU = q.Employee.EMP_HGH_EDU,
                        EM_WORK_EXP = q.Employee.EM_WORK_EXP,
                        EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                        EMP_IC_OLD = q.Employee.EMP_IC_OLD,
                        EMP_IC_NEW = q.Employee.EMP_IC_NEW,
                        EMP_IC_COLOR = q.Employee.EMP_IC_COLOR,
                        EMP_PASSPORT_NO = q.Employee.EMP_PASSPORT_NO,
                        EMP_SEX = q.Employee.EMP_SEX,
                        EMP_RACE = q.Employee.EMP_RACE,
                        EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                        EMP_SPOUSE_NAME = q.Employee.EMP_SPOUSE_NAME,
                        EMP_SP_IC = q.Employee.EMP_SP_IC,
                        EMP_NO_CHILD = q.Employee.EMP_NO_CHILD,
                        EMP_SP_WORK = q.Employee.EMP_SP_WORK,
                        EMP_PER_NAME_CONTACT = q.Employee.EMP_PER_NAME_CONTACT,
                        EMP_CONTACT_ADDRESS1 = q.Employee.EMP_CONTACT_ADDRESS1,
                        EMP_CONTACT_ADDRESS2 = q.Employee.EMP_CONTACT_ADDRESS2,
                        EMP_CONTACT_POST_CODE = q.Employee.EMP_CONTACT_POST_CODE,
                        EMP_CONTACT_TOWN = q.Employee.EMP_CONTACT_TOWN,
                        EMP_CONTACT_STATE = q.Employee.EMP_CONTACT_STATE,
                        EMP_CONTACT_TELEPHONE = q.Employee.EMP_CONTACT_TELEPHONE,
                        EMP_BRANCH_CODE = q.Employee.EMP_BRANCH_CODE,
                        OldBranch = q.Employee.OldBranch,
                        TransferDate = q.Employee.TransferDate,
                        LASTUPDATE = q.Employee.LASTUPDATE,
                        NewSalaryStructure = q.Employee.NewSalaryStructure,
                        SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                    })
                    .ToList();
            }
            else
            {
                var query = from e in _oBMSDbContext.Employees
                            join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
                            where (e.HasTransfered == false)
                            select new
                            {
                                Employee = e,
                                EmploymentDetail = ed
                            };

                // Apply filters before projection
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Active")
                    {
                        if (resignedDate.Month == 12)
                        {
                            query = query.Where(q =>
                           q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                           (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                        else
                        {
                            query = query.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                    }
                    else if (status == "Inactive")
                    {
                        query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                    }
                }

                if (!string.IsNullOrEmpty(employeeType))
                    query = query.Where(q => q.Employee.EMP_ROLE.Contains(employeeType));

                if (!string.IsNullOrEmpty(branch))
                    query = query.Where(q => q.Employee.EMP_BRANCH_CODE == branch);

                if (resignedDate.Year != 1)
                    query = query.Where(q =>
                        q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null || q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

                if (joinDate.Year != 1)
                    query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

                query = query.OrderBy(emp => emp.Employee.EMP_NAME);

                // Project the results into EmployeeDto
                result = query
                    .OrderBy(q => q.Employee.EMP_NAME)
                    .Select(q => new EmployeeDto
                    {
                        EMP_ID = q.Employee.EMP_ID,
                        EMP_ROLE = q.Employee.EMP_ROLE,
                        EMP_CODE = q.Employee.EMP_CODE,
                        EMP_NAME = q.Employee.EMP_NAME,
                        EMP_CLIENT = q.Employee.EMP_CLIENT,
                        EMP_ADDRESS1 = q.Employee.EMP_ADDRESS1,
                        EMP_ADDRESS2 = q.Employee.EMP_ADDRESS2,
                        EMP_POST_CODE = q.Employee.EMP_POST_CODE,
                        EMP_TOWN = q.Employee.EMP_TOWN,
                        EMP_STATE = q.Employee.EMP_STATE,
                        EMP_NATIONAL = q.Employee.EMP_NATIONAL,
                        EMP_PHONE = q.Employee.EMP_PHONE,
                        EMP_MOBILEPHONE = q.Employee.EMP_MOBILEPHONE,
                        EMP_CITIZEN = q.Employee.EMP_CITIZEN,
                        EMP_CHECKLIST = q.Employee.EMP_CHECKLIST,
                        EMP_HGH_EDU = q.Employee.EMP_HGH_EDU,
                        EM_WORK_EXP = q.Employee.EM_WORK_EXP,
                        EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                        EMP_IC_OLD = q.Employee.EMP_IC_OLD,
                        EMP_IC_NEW = q.Employee.EMP_IC_NEW,
                        EMP_IC_COLOR = q.Employee.EMP_IC_COLOR,
                        EMP_PASSPORT_NO = q.Employee.EMP_PASSPORT_NO,
                        EMP_SEX = q.Employee.EMP_SEX,
                        EMP_RACE = q.Employee.EMP_RACE,
                        EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                        EMP_SPOUSE_NAME = q.Employee.EMP_SPOUSE_NAME,
                        EMP_SP_IC = q.Employee.EMP_SP_IC,
                        EMP_NO_CHILD = q.Employee.EMP_NO_CHILD,
                        EMP_SP_WORK = q.Employee.EMP_SP_WORK,
                        EMP_PER_NAME_CONTACT = q.Employee.EMP_PER_NAME_CONTACT,
                        EMP_CONTACT_ADDRESS1 = q.Employee.EMP_CONTACT_ADDRESS1,
                        EMP_CONTACT_ADDRESS2 = q.Employee.EMP_CONTACT_ADDRESS2,
                        EMP_CONTACT_POST_CODE = q.Employee.EMP_CONTACT_POST_CODE,
                        EMP_CONTACT_TOWN = q.Employee.EMP_CONTACT_TOWN,
                        EMP_CONTACT_STATE = q.Employee.EMP_CONTACT_STATE,
                        EMP_CONTACT_TELEPHONE = q.Employee.EMP_CONTACT_TELEPHONE,
                        EMP_BRANCH_CODE = q.Employee.EMP_BRANCH_CODE,
                        OldBranch = q.Employee.OldBranch,
                        TransferDate = q.Employee.TransferDate,
                        LASTUPDATE = q.Employee.LASTUPDATE,
                        NewSalaryStructure = q.Employee.NewSalaryStructure,
                        SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                    })
                    .ToList();
            }
            return result;
        }

        public List<EmployeeDto> GetListEmployeeByClient(string branch, string employeeType, DateTime resignedDate, DateTime joinDate, string status, string empClient)
        {
            var defaultResignedDate = new DateTime(2100, 1, 1);
            var result = new List<EmployeeDto>();
            // Base query
            if (joinDate.Year != 1)
            {
                var query = from e in _oBMSDbContext.Employees
                            join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
                            where (e.HasTransfered == false ||
                                  (e.HasTransfered == true && e.TransferDate >= resignedDate))
                            select new
                            {
                                Employee = e,
                                EmploymentDetail = ed
                            };

                // Apply filters before projection

                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Active")
                    {
                        if (resignedDate.Month == 12)
                        {
                            query = query.Where(q =>
                           q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                           (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                        else
                        {
                            query = query.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                    }
                    else if (status == "Inactive")
                    {
                        query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                    }
                }

                if (!string.IsNullOrEmpty(employeeType))
                    query = query.Where(q => q.Employee.EMP_ROLE == employeeType);

                if (!string.IsNullOrEmpty(empClient))
                    query = query.Where(q => q.Employee.EMP_CLIENT == empClient);

                if (!string.IsNullOrEmpty(branch))
                    query = query.Where(q => q.Employee.EMP_BRANCH_CODE == branch);

                if (resignedDate.Year != 1)
                    query = query.Where(q =>
                        q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null || q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

                if (joinDate.Year != 1)
                    query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

                query = query.OrderBy(emp => emp.Employee.EMP_NAME);

                // Project the results into EmployeeDto
                result = query
                    .OrderBy(q => q.Employee.EMP_NAME)
                    .Select(q => new EmployeeDto
                    {
                        EMP_ID = q.Employee.EMP_ID,
                        EMP_ROLE = q.Employee.EMP_ROLE,
                        EMP_CODE = q.Employee.EMP_CODE,
                        EMP_NAME = string.IsNullOrEmpty(q.Employee.EMP_NAME)
                            ? q.Employee.EMP_NAME
                            : q.Employee.EMP_NAME.Replace("''", "'"),
                        EMP_CLIENT = q.Employee.EMP_CLIENT,
                        EMP_ADDRESS1 = q.Employee.EMP_ADDRESS1,
                        EMP_ADDRESS2 = q.Employee.EMP_ADDRESS2,
                        EMP_POST_CODE = q.Employee.EMP_POST_CODE,
                        EMP_TOWN = q.Employee.EMP_TOWN,
                        EMP_STATE = q.Employee.EMP_STATE,
                        EMP_NATIONAL = q.Employee.EMP_NATIONAL,
                        EMP_PHONE = q.Employee.EMP_PHONE,
                        EMP_MOBILEPHONE = q.Employee.EMP_MOBILEPHONE,
                        EMP_CITIZEN = q.Employee.EMP_CITIZEN,
                        EMP_CHECKLIST = q.Employee.EMP_CHECKLIST,
                        EMP_HGH_EDU = q.Employee.EMP_HGH_EDU,
                        EM_WORK_EXP = q.Employee.EM_WORK_EXP,
                        EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                        EMP_IC_OLD = q.Employee.EMP_IC_OLD,
                        EMP_IC_NEW = q.Employee.EMP_IC_NEW,
                        EMP_IC_COLOR = q.Employee.EMP_IC_COLOR,
                        EMP_PASSPORT_NO = q.Employee.EMP_PASSPORT_NO,
                        EMP_SEX = q.Employee.EMP_SEX,
                        EMP_RACE = q.Employee.EMP_RACE,
                        EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                        EMP_SPOUSE_NAME = q.Employee.EMP_SPOUSE_NAME,
                        EMP_SP_IC = q.Employee.EMP_SP_IC,
                        EMP_NO_CHILD = q.Employee.EMP_NO_CHILD,
                        EMP_SP_WORK = q.Employee.EMP_SP_WORK,
                        EMP_PER_NAME_CONTACT = q.Employee.EMP_PER_NAME_CONTACT,
                        EMP_CONTACT_ADDRESS1 = q.Employee.EMP_CONTACT_ADDRESS1,
                        EMP_CONTACT_ADDRESS2 = q.Employee.EMP_CONTACT_ADDRESS2,
                        EMP_CONTACT_POST_CODE = q.Employee.EMP_CONTACT_POST_CODE,
                        EMP_CONTACT_TOWN = q.Employee.EMP_CONTACT_TOWN,
                        EMP_CONTACT_STATE = q.Employee.EMP_CONTACT_STATE,
                        EMP_CONTACT_TELEPHONE = q.Employee.EMP_CONTACT_TELEPHONE,
                        EMP_BRANCH_CODE = q.Employee.EMP_BRANCH_CODE,
                        OldBranch = q.Employee.OldBranch,
                        TransferDate = q.Employee.TransferDate,
                        LASTUPDATE = q.Employee.LASTUPDATE,
                        NewSalaryStructure = q.Employee.NewSalaryStructure,
                        SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                    })
                    .ToList();
            }
            else
            {
                var query = from e in _oBMSDbContext.Employees
                            join ed in _oBMSDbContext.EmploymentDetails on e.EMP_CODE equals ed.EMPPAY_CODE
                            join esd in _oBMSDbContext.EmployeeSalaryDetails on e.EMP_CODE equals esd.EMPFL_CODE
                            where (e.HasTransfered == false)
                            select new
                            {
                                Employee = e,
                                EmploymentDetail = ed
                            };

                // Apply filters before projection
                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "Active")
                    {
                        if (resignedDate.Month == 12)
                        {
                            query = query.Where(q =>
                           q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                           (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year - 1 &&
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                        else
                        {
                            query = query.Where(q =>
                            q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null ||
                            (q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Year >= resignedDate.Year &&
                             q.EmploymentDetail.EMPPAY_DATE_RESIGNED.Value.Month >= resignedDate.Month - 1));
                        }
                    }
                    else if (status == "Inactive")
                    {
                        query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_RESIGNED != null);
                    }
                }

                if (!string.IsNullOrEmpty(employeeType))
                    query = query.Where(q => q.Employee.EMP_ROLE.Contains(employeeType));

                if (!string.IsNullOrEmpty(branch))
                    query = query.Where(q => q.Employee.EMP_BRANCH_CODE == branch);

                if (resignedDate.Year != 1)
                    query = query.Where(q =>
                        q.EmploymentDetail.EMPPAY_DATE_RESIGNED == null || q.EmploymentDetail.EMPPAY_DATE_RESIGNED >= resignedDate);

                if (joinDate.Year != 1)
                    query = query.Where(q => q.EmploymentDetail.EMPPAY_DATE_JOINED <= joinDate);

                query = query.OrderBy(emp => emp.Employee.EMP_NAME);

                // Project the results into EmployeeDto
                result = query
                    .OrderBy(q => q.Employee.EMP_NAME)
                    .Select(q => new EmployeeDto
                    {
                        EMP_ID = q.Employee.EMP_ID,
                        EMP_ROLE = q.Employee.EMP_ROLE,
                        EMP_CODE = q.Employee.EMP_CODE,                       
                        EMP_NAME = string.IsNullOrEmpty(q.Employee.EMP_NAME)
                            ? q.Employee.EMP_NAME
                            : q.Employee.EMP_NAME.Replace("''", "'"),
                        EMP_CLIENT = q.Employee.EMP_CLIENT,
                        EMP_ADDRESS1 = q.Employee.EMP_ADDRESS1,
                        EMP_ADDRESS2 = q.Employee.EMP_ADDRESS2,
                        EMP_POST_CODE = q.Employee.EMP_POST_CODE,
                        EMP_TOWN = q.Employee.EMP_TOWN,
                        EMP_STATE = q.Employee.EMP_STATE,
                        EMP_NATIONAL = q.Employee.EMP_NATIONAL,
                        EMP_PHONE = q.Employee.EMP_PHONE,
                        EMP_MOBILEPHONE = q.Employee.EMP_MOBILEPHONE,
                        EMP_CITIZEN = q.Employee.EMP_CITIZEN,
                        EMP_CHECKLIST = q.Employee.EMP_CHECKLIST,
                        EMP_HGH_EDU = q.Employee.EMP_HGH_EDU,
                        EM_WORK_EXP = q.Employee.EM_WORK_EXP,
                        EMP_DATE_OF_BIRTH = q.Employee.EMP_DATE_OF_BIRTH,
                        EMP_IC_OLD = q.Employee.EMP_IC_OLD,
                        EMP_IC_NEW = q.Employee.EMP_IC_NEW,
                        EMP_IC_COLOR = q.Employee.EMP_IC_COLOR,
                        EMP_PASSPORT_NO = q.Employee.EMP_PASSPORT_NO,
                        EMP_SEX = q.Employee.EMP_SEX,
                        EMP_RACE = q.Employee.EMP_RACE,
                        EMP_MARTIAL_STATUS = q.Employee.EMP_MARTIAL_STATUS,
                        EMP_SPOUSE_NAME = q.Employee.EMP_SPOUSE_NAME,
                        EMP_SP_IC = q.Employee.EMP_SP_IC,
                        EMP_NO_CHILD = q.Employee.EMP_NO_CHILD,
                        EMP_SP_WORK = q.Employee.EMP_SP_WORK,
                        EMP_PER_NAME_CONTACT = q.Employee.EMP_PER_NAME_CONTACT,
                        EMP_CONTACT_ADDRESS1 = q.Employee.EMP_CONTACT_ADDRESS1,
                        EMP_CONTACT_ADDRESS2 = q.Employee.EMP_CONTACT_ADDRESS2,
                        EMP_CONTACT_POST_CODE = q.Employee.EMP_CONTACT_POST_CODE,
                        EMP_CONTACT_TOWN = q.Employee.EMP_CONTACT_TOWN,
                        EMP_CONTACT_STATE = q.Employee.EMP_CONTACT_STATE,
                        EMP_CONTACT_TELEPHONE = q.Employee.EMP_CONTACT_TELEPHONE,
                        EMP_BRANCH_CODE = q.Employee.EMP_BRANCH_CODE,
                        OldBranch = q.Employee.OldBranch,
                        TransferDate = q.Employee.TransferDate,
                        LASTUPDATE = q.Employee.LASTUPDATE,
                        NewSalaryStructure = q.Employee.NewSalaryStructure,
                        SalaryStructure1000_3h = q.Employee.SalaryStructure1000_3h
                    })
                    .ToList();
            }
            return result;
        }

        public async Task<List<SalaryAdvanceDto>> GetListByEmplyeeType(DateTime advanceDate, string branch, string employeeType, int transType, decimal advanceAmount, string race)
        {
            if (race == "All")
            {
                string[] raceArray = { "Chinese", "Indian", "Malay", "Others" };
                race = "('" + string.Join("','", raceArray) + "')";

            }

            var query = (from employee in _oBMSDbContext.Employees
                         join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                             on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                         join employmentDetails in _oBMSDbContext.EmploymentDetails
                             on employee.EMP_CODE equals employmentDetails.EMPPAY_CODE
                         join salaryAdvanceTemp in _oBMSDbContext.SalaryAdvances
                             .Where(sa => sa.TransType == transType
                                          && !sa.IsDeleted
                                          && sa.AdvanceDate.Month == advanceDate.Month
                                          && sa.AdvanceDate.Year == advanceDate.Year)
                             on employee.EMP_ID equals salaryAdvanceTemp.EmployeeID into salaryAdvanceGroup
                         from salaryAdvance in salaryAdvanceGroup.DefaultIfEmpty()
                         where employee.EMP_ROLE == employeeType
                              && employee.EMP_BRANCH_CODE == branch
                              && employee.HasTransfered == false
                         //&& !salaryAdvance.IsDeleted
                          && race.Contains(employee.EMP_RACE)

                         orderby employee.EMP_NAME
                         select new
                         {
                             employee,
                             salaryDetails,
                             employmentDetails,
                             salaryAdvance
                         }).AsEnumerable() // <-- Forces execution in memory
             .Where(x =>
             {
                 DateTime joinedDate = Convert.ToDateTime(x.employmentDetails.EMPPAY_DATE_JOINED);
                 DateTime resignedDate = string.IsNullOrEmpty(x.employmentDetails.EMPPAY_DATE_RESIGNED.ToString())
                                         ? new DateTime(2100, 1, 1)
                                         : Convert.ToDateTime(x.employmentDetails.EMPPAY_DATE_RESIGNED);
                 DateTime compareDate = x.salaryAdvance?.AdvanceDate ?? advanceDate;

                 return joinedDate <= compareDate && resignedDate >= compareDate;
             })
             .Select(x => new SalaryAdvanceDto
             {
                 ID = x.salaryAdvance?.ID ?? 0,
                 EMP_ID = x.employee.EMP_ID,
                 EMP_CODE = x.employee.EMP_CODE,
                 EMP_NAME = string.IsNullOrEmpty(x.employee.EMP_NAME)
                            ? x.employee.EMP_NAME
                            : x.employee.EMP_NAME.Replace("''", "'"),
                 EMP_RACE = x.employee.EMP_RACE,
                 EMP_ROLE = x.employee.EMP_ROLE,
                 EMP_IC_NEW = x.employee.EMP_IC_NEW ?? "",
                 EMP_IC_OLD = x.employee.EMP_IC_OLD ?? "",
                 EMP_PASSPORT_NO = x.employee.EMP_PASSPORT_NO ?? "",
                 EMPFL_BANK = x.salaryDetails.EMPFL_BANK ?? "",
                 EMPFL_BK_ACCNO = x.salaryDetails.EMPFL_BK_ACCNO ?? "",
                 PAYMODE = x.salaryDetails.PAYMODE ?? "",
                 Amount = x.salaryAdvance?.Amount ?? 0,
                 Particulars = x.salaryAdvance?.Particulars ?? ""
             })
             .Distinct()
             .ToList();        


            return new List<SalaryAdvanceDto>(query);

        }

        public async Task<List<SalaryAdvanceDto>> GetEmployeeAdvanceList(DateTime advanceDate, string branch, string employeeType, string client, int transType, decimal advanceAmount, string race)
        {
            // Handle race filter properly
            List<string> raceList = null;
            if (race == "All")
            {
                raceList = new List<string> { "Chinese", "Indian", "Malay", "Others" };
            }
            else if (!string.IsNullOrEmpty(race))
            {
                raceList = new List<string> { race };
            }

            var query = from employee in _oBMSDbContext.Employees
                        join salaryDetails in _oBMSDbContext.EmployeeSalaryDetails
                            on employee.EMP_CODE equals salaryDetails.EMPFL_CODE
                        join employmentDetails in _oBMSDbContext.EmploymentDetails
                            on employee.EMP_CODE equals employmentDetails.EMPPAY_CODE
                        join salaryAdvanceTemp in _oBMSDbContext.SalaryAdvances
                            .Where(sa => sa.TransType == transType
                                         && !sa.IsDeleted
                                         && sa.AdvanceDate.Month == advanceDate.Month
                                         && sa.AdvanceDate.Year == advanceDate.Year)
                            on employee.EMP_ID equals salaryAdvanceTemp.EmployeeID into salaryAdvanceGroup
                        from salaryAdvance in salaryAdvanceGroup.DefaultIfEmpty()
                        where employee.EMP_ROLE == employeeType
                              && employee.EMP_BRANCH_CODE == branch
                              && employee.HasTransfered == false
                              && employee.EMP_CLIENT == client
                              && (raceList == null || raceList.Contains(employee.EMP_RACE))
                        orderby employee.EMP_NAME
                        select new
                        {
                            employee,
                            salaryDetails,
                            employmentDetails,
                            salaryAdvance
                        };

            var data =  query
                .AsEnumerable() // keep date comparison logic same as old SQL
                .Where(x =>
                {
                    DateTime joinedDate = x.employmentDetails.EMPPAY_DATE_JOINED;
                    DateTime resignedDate = x.employmentDetails.EMPPAY_DATE_RESIGNED ?? new DateTime(2100, 1, 1);
                    DateTime compareDate = x.salaryAdvance?.AdvanceDate ?? advanceDate;

                    return joinedDate <= compareDate && resignedDate >= compareDate;
                })
                .Select(x => new SalaryAdvanceDto
                {
                    ID = x.salaryAdvance?.ID ?? 0,
                    EMP_ID = x.employee.EMP_ID,
                    EMP_CODE = x.employee.EMP_CODE,
                    EMP_NAME = string.IsNullOrEmpty(x.employee.EMP_NAME)
                            ? x.employee.EMP_NAME
                            : x.employee.EMP_NAME.Replace("''", "'"),
                    EMP_RACE = x.employee.EMP_RACE,
                    EMP_ROLE = x.employee.EMP_ROLE,
                    EMP_IC_NEW = x.employee.EMP_IC_NEW ?? "",
                    EMP_IC_OLD = x.employee.EMP_IC_OLD ?? "",
                    EMP_PASSPORT_NO = x.employee.EMP_PASSPORT_NO ?? "",
                    EMPFL_BANK = x.salaryDetails.EMPFL_BANK ?? "",
                    EMPFL_BK_ACCNO = x.salaryDetails.EMPFL_BK_ACCNO ?? "",
                    PAYMODE = x.salaryDetails.PAYMODE ?? "",
                    Amount = x.salaryAdvance?.Amount ?? advanceAmount,
                    Particulars = x.salaryAdvance?.Particulars ?? ""
                })
                .Distinct()
                .ToList();

            return data;
        }


        #endregion

        #region Salary Processing
        public string LastSalaryProcessRemarks(DateTime period, string branchCode, string employeeType)
        {
            var remarks = _oBMSDbContext.SalaryProcess
                 .Where(sp => sp.Period <= period && sp.Branch == branchCode && sp.EmployeeType == employeeType)
                 .Select(sp => sp.Remarks)
                 .FirstOrDefault();

            return remarks;
        }
        public bool IsSalaryProcessDoneForCurrentPeriod(string branch, string employeeType, DateTime dtPeriod)
        {
            var isProcessed = _oBMSDbContext.SalaryProcess
                   .Any(sp =>
                       sp.EmployeeType == employeeType &&
                       sp.Branch == branch &&
                       sp.Period.Year == dtPeriod.Year &&
                       sp.Period.Month == dtPeriod.Month);

            return isProcessed;
        }
        public List<string> GetEmployeeAttendanceList(DateTime period, string branch)
        {
            var nameList = _oBMSDbContext.Attendances
                .Where(a => a.Period == period && a.Branch == branch)
                .Join(
                    _oBMSDbContext.Employees,
                    attendance => attendance.EmployeeID,
                    employee => employee.EMP_ID,
                    (attendance, employee) => employee.EMP_CODE
                )
                .ToList();

            return nameList;

        }
        public bool IsTemporaryEmployee(string employeeCode)
        {
            try
            {
                var employee = _oBMSDbContext.EmployeeSalaryDetails
                    .Where(e => e.EMPFL_CODE == employeeCode)
                    .FirstOrDefault();

                return employee?.TMPGUARD ?? false;
            }
            catch
            {
                throw;
            }
        }
        public string Process(string branch, string employeeType, string remarks, DateTime period, bool lockProcess, string currentUser, string companyCode)
        {
            string status = string.Empty;
            var attendanceQuery = (from attendance in _oBMSDbContext.Attendances
                                   join employee in _oBMSDbContext.Employees on attendance.EmployeeID equals employee.EMP_ID
                                   where attendance.Branch == branch && employee.EMP_ROLE.Contains(employeeType) && attendance.Period == period
                                   orderby employee.EMP_NAME
                                   select new { attendance.Branch, attendance.EmployeeID }).ToList();
            List<int> employeeIDs = attendanceQuery.Select(a => a.EmployeeID).ToList();
            var salaryProcess = _oBMSDbContext.SalaryProcess
                    .FirstOrDefault(sp => sp.Period == period && sp.Branch == branch && sp.EmployeeType.Contains(employeeType));

            if (salaryProcess != null)
            {
                if (salaryProcess.IsLocked)
                    return "Salary Processing for the month is locked. It cannot be recomputed again.";

                salaryProcess.ID = salaryProcess.ID;
                salaryProcess.LastUpdate = DateTime.Now;
                salaryProcess.LastUpdatedBy = currentUser;
                salaryProcess.IsLocked = lockProcess;
                salaryProcess.Remarks = remarks;
                _oBMSDbContext.SalaryProcess.Update(salaryProcess);
                status = "updated";
            }
            else
            {
                _oBMSDbContext.SalaryProcess.Add(new SalaryProcess
                {
                    Branch = branch,
                    Period = period,
                    EmployeeType = employeeType,
                    IsLocked = lockProcess,
                    Remarks = remarks,
                    LastUpdate = DateTime.Now,
                    LastUpdatedBy = currentUser
                });
                status = "inserted";
            }

            //  _oBMSDbContext.SaveChanges();
            if (attendanceQuery != null)
            {
                decimal dAttendanceAllowance = 0;
                decimal dSpecialAllowance = 0;
                decimal dAttendanceAllowanceDays = 0;
                string sAttendanceAllowanceFollowCalendar = string.Empty;
                decimal dReAllowance = 0;
                decimal dReAllowanceRate = 0;
                var query = from employmentDetails in _oBMSDbContext.EmploymentDetails
                            join employee in _oBMSDbContext.Employees on employmentDetails.EMPPAY_CODE equals employee.EMP_CODE
                            join employeeSalaryDetails in _oBMSDbContext.EmployeeSalaryDetails on employee.EMP_CODE equals employeeSalaryDetails.EMPFL_CODE
                            join salaryStructure in _oBMSDbContext.SalaryStructure on employmentDetails.SALARYLAB equals salaryStructure.SalaryId
                            where employeeIDs.Contains(employee.EMP_ID)
                            select new
                            {
                                employeeSalaryDetails.TMPGUARD,
                                employee.EMP_DATE_OF_BIRTH,
                                employee.EMP_CITIZEN,
                                employmentDetails.EMPPAY_DATE_JOINED,
                                employmentDetails.EMPPAY_DATE_RESIGNED,
                                employmentDetails.EMPPAY_BASIC_RATE,
                                employmentDetails.ATTENDANCEALLOWANCE,
                                employmentDetails.SpecialAllowance,
                                employmentDetails.AttendanceAllowanceWorkingDays,
                                employmentDetails.AttendanceAllowanceFollowCalendar,
                                salaryStructure.EmployeeNationality,
                                salaryStructure.WorkingHours,
                                salaryStructure.Name,
                                salaryStructure.TravelAllowance,
                                salaryStructure.GeneralDayRate,
                                salaryStructure.GeneralDayHours,
                                salaryStructure.WorkingDays,
                                salaryStructure.GeneralDayOTRate,
                                salaryStructure.OffDayRate,
                                salaryStructure.OffDayOTRate,
                                salaryStructure.HolidayRate,
                                salaryStructure.HolidayOTRate,
                                salaryStructure.SalaryBand,
                                salaryStructure.EICC,
                                salaryStructure.NonStructure,
                                employee.NewSalaryStructure,
                                employee.SalaryStructure1000_3h,
                                employee.EMP_PASSPORT_NO
                            };

                var drEmployee = query.FirstOrDefault();

                bool DeductEPF8Pa = false;
                bool DeductEPF = false;
                bool DeductSOCSO = false;
                bool DeductEPFBeyond55 = false;
                bool DeductIncomeTax = false; //IncomeTax
                decimal WorkingHours = 0;
                decimal GeneralDayHours = 0;
                int EmployeeAge = 0;
                int strCitizen = 0;
                decimal NoOfWorkingDays = 0;
                string SalaryPayMode = string.Empty;
                string EmployeeNationality = string.Empty;
                decimal dAttendanceDeduction = 0;
                decimal dSpecialAttendanceDeduction = 0;
                double dEmployeeBasicRate = 0;
                decimal dBonus = 0;
                int sSalaryStructure1000_3h = 0;
                string sEmpPassport = string.Empty;
                bool bNonStructure = false;
                bool bTmpGuard = false;

                if (drEmployee != null)
                {
                    //EmployeeAge = period.Year - drEmployee.EMP_DATE_OF_BIRTH.Year;
                    //if (period.DayOfYear < drEmployee.EMP_DATE_OF_BIRTH.DayOfYear)
                    //{
                    //    EmployeeAge = EmployeeAge - 1;
                    //}
                    WorkingHours = drEmployee.WorkingHours;
                    GeneralDayHours = drEmployee.GeneralDayHours;
                    NoOfWorkingDays = drEmployee.WorkingDays;
                    dAttendanceAllowance = (decimal)drEmployee.ATTENDANCEALLOWANCE;
                    dSpecialAllowance = drEmployee.SpecialAllowance;
                    EmployeeNationality = drEmployee.EmployeeNationality;
                    sAttendanceAllowanceFollowCalendar = drEmployee.AttendanceAllowanceFollowCalendar;
                    dAttendanceAllowanceDays = (decimal)drEmployee.AttendanceAllowanceWorkingDays;
                    dEmployeeBasicRate = drEmployee.EMPPAY_BASIC_RATE;
                    strCitizen = drEmployee.EMP_CITIZEN;
                    sEmpPassport = drEmployee.EMP_PASSPORT_NO;
                    bNonStructure = drEmployee.NonStructure;
                    bTmpGuard = drEmployee.TMPGUARD;

                }

                var drAllowanceDeduct = _oBMSDbContext.Attendances
                           .Where(a => employeeIDs.Contains(a.EmployeeID) &&
                                       a.Period.Month == period.Month &&
                                       a.Period.Year == period.Year)
                           .Select(a => new
                           {
                               a.AllowanceDeduction,
                               a.SpecialAllowanceDeduction
                           }).FirstOrDefault();

                if (drAllowanceDeduct != null)
                {
                    if (employeeType == "Staff")
                    {
                        //Normal Allowance
                        dAttendanceAllowance -= drAllowanceDeduct.AllowanceDeduction;
                        dAttendanceDeduction = drAllowanceDeduct.AllowanceDeduction;

                        //Special Allowance deduction
                        dSpecialAttendanceDeduction = drAllowanceDeduct.SpecialAllowanceDeduction;
                        dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;

                    }
                    else
                    {
                        //Normal Allowance
                        dAttendanceDeduction = drAllowanceDeduct.AllowanceDeduction;
                        dSpecialAttendanceDeduction = drAllowanceDeduct.SpecialAllowanceDeduction;

                        //Special Allowance deduction
                        dSpecialAllowance = dSpecialAllowance - dSpecialAttendanceDeduction;

                    }
                }

                decimal OTGeneralDayHours = GeneralDayHours;
                OTGeneralDayHours = OTGeneralDayHours + Convert.ToDecimal(sSalaryStructure1000_3h);

                if (EmployeeNationality == "F")
                {

                }
                else
                {

                }
            }
            return status;
        }
        public async Task<DateTime?> GetLatestAttendancePeriodAsync(int employeeId, int year, int month)
        {
            var record = await _oBMSDbContext.Attendances
                .Where(a => a.EmployeeID == employeeId &&
                            a.Period.Year == year &&
                            a.Period.Month > month)
                .OrderBy(a => a.Period)
                .FirstOrDefaultAsync();

            return record?.Period;
        }

        public async Task<List<MiscTransDto>> GetList(DateTime transDate, decimal employeeId)
        {
            try
            {
                return await _oBMSDbContext.MiscTrans
                    .Where(x => x.EmployeeID == employeeId &&
                                x.TransDate.Month == transDate.Month &&
                                x.TransDate.Year == transDate.Year)
                    .Select(x => new MiscTransDto
                    {
                        ID = x.ID,
                        EmployeeID = x.EmployeeID,
                        TransDate = x.TransDate,
                        Amount = x.Amount,
                        TransType = x.TransType,
                        Particulars = x.Particulars ?? "",
                        LastUpdate = x.LastUpdate
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving Misc Transactions", ex);
            }
        }

        public async Task<List<AttendanceDetailsDto>> GetAttendanceDetailsByEmployee(int employeeId)
        {
            var attendanceIds = await _oBMSDbContext.Attendances
                .Where(a => a.EmployeeID == employeeId)
                .Select(a => a.ID)
                .ToListAsync();

            var details = await _oBMSDbContext.AttendanceDetails
                .Where(d => attendanceIds.Contains(d.AttendanceID))
                .Select(d => new AttendanceDetailsDto
                {
                    ID = d.ID,
                    AttendanceID = d.AttendanceID,
                    AttendanceDate = d.AttendanceDate,
                    Client = d.Client,
                    TimeStart = d.TimeStart,
                    TimeEnd = d.TimeEnd,
                    OTClient = d.OTClient,
                    OTTimeStart = d.OTTimeStart,
                    OTTimeEnd = d.OTTimeEnd,
                    Type = d.Type,
                    LastUpdate = d.LastUpdate,
                    LastUpdatedBy = d.LastUpdatedBy
                })
                .ToListAsync();

            return details;
        }
        #endregion
    }
}
