using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;
using SkiaSharp;

namespace OBMS.WebAPI.Repositories.Implementation
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly OBMSDbContext _oBMSDbContext;

        public EmployeeRepository(OBMSDbContext oBMSDbContext)
        {
            _oBMSDbContext = oBMSDbContext;

        }
        public async Task<Dictionary<string, string>> GetEmployeeNoByBranchID(string clientId)
        {
            var results = new Dictionary<string, string>();

            var result = (from cm in _oBMSDbContext.ClientMasters
                          where cm.Code == clientId
                          join emp in _oBMSDbContext.Employees
                          on cm.Code equals emp.EMP_CLIENT into empGroup
                          from emp in empGroup.DefaultIfEmpty()
                          group new { cm, emp } by new { cm.Shortname } into grouped
                          select new
                          {
                              ShortName = grouped.Key.Shortname.Trim(),
                              NextEmployeeCode = Convert.ToInt32(
                                  grouped.Max(g => g.emp != null ? g.emp.EMP_CODE : g.cm.Shortname)
                                        .Substring(grouped.Key.Shortname.Length + 1, 6))
                          }).FirstOrDefault();

            if (result != null)
            {
                string shortName = result.ShortName;
                int nextEmployeeCode = result.NextEmployeeCode + 1;
                results.Add("ShortName", shortName);
                results.Add("Code", nextEmployeeCode.ToString("D6"));
                return results;
            }
            else
            {
                return null;
            }
        }


        public async Task<Dictionary<string, string>> GetEmployeeNo()
        {
            var results = new Dictionary<string, string>();

            string nextEmployeeCode = _oBMSDbContext.Employees
                    .OrderByDescending(emp => emp.EMP_ID)
                    .Select(emp => emp.EMP_CODE)
                    .Where(empCode => empCode != null)
                    .FirstOrDefault();
            var company = Utility.Utility.GetCompnay();
            string shortName = company["ShortName"];

            if (nextEmployeeCode != null && nextEmployeeCode !="")
            {
                
                int numericPart = int.Parse(nextEmployeeCode.Substring(shortName.Length + 1, 6)) + 1;


                results.Add("ShortName", shortName);

                results.Add("Code", numericPart.ToString("D6"));
                return results;
            }
            else
            {
                results.Add("ShortName", shortName);

                results.Add("Code", "000001");
                return results;
            }
        }

        #region Employee Master Module

        public async Task<Dictionary<string, Object>> GetEmployeeById(int employeeId)
        {

            var results = new Dictionary<string, Object>();

            if (employeeId != null)
            {

                var employee = _oBMSDbContext.Employees.Where(x => x.EMP_ID == employeeId).FirstOrDefault();
                results.Add("employee", employee);

                var employment = _oBMSDbContext.EmploymentDetails.Where(x => x.EMPPAY_CODE == employee.EMP_CODE).FirstOrDefault();
                results.Add("employment", employment);

                var salaryDetail = _oBMSDbContext.EmployeeSalaryDetails.Where(x => x.EMPFL_CODE == employee.EMP_CODE).FirstOrDefault();

                results.Add("salaryDetail", salaryDetail);

            }

            return results;
        }
        public async Task<Dictionary<string, Object>> GetEmployeeMasterList(string userID)
        {

            var results = new Dictionary<string, Object>();
            bool isSuperAdmin = string.Equals(userID.Trim().ToLower(), "superadmin", StringComparison.OrdinalIgnoreCase);

            List<BranchMaster> branchList;
            List<BankList> bankList;

            if (isSuperAdmin)
            {
                // SuperAdmin → get all branches
                branchList = await _oBMSDbContext.BranchMasters
                    .Select(x => new BranchMaster
                    {
                        ID = x.ID,
                        Code = x.Code,
                        Name = x.Name,
                        Address1 = x.Address1,
                        Address2 = x.Address2,
                        PostCode = x.PostCode,
                        City = x.City,
                        State = x.State,
                        Phone = x.Phone,
                        Fax = x.Fax,
                        BankName = x.BankName,
                        BankBranch = x.BankBranch,
                        BankAccount = x.BankAccount,
                        PersonIncharge = x.PersonIncharge,
                        Email = x.Email,
                        Description = x.Description,
                        ShortName = x.ShortName,
                        IsHeadQuarters = x.IsHeadQuarters,
                        UbsCode = x.UbsCode,
                        LastUpdate = x.LastUpdate,
                        LastUpdatedBy = x.LastUpdatedBy,
                        ParentBranch = x.ParentBranch
                    })
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                // Normal user → join filter
                branchList = await _oBMSDbContext.BranchMasters
                    .Join(_oBMSDbContext.OBMSBranches,
                        bm => bm.Code,
                        ob => ob.BranchCode,
                        (bm, ob) => new { bm, ob })
                    .Where(x => x.ob.Name == userID && x.ob.IsAllowed == true)
                    .Select(x => new BranchMaster
                    {
                        ID = x.bm.ID,
                        Code = x.bm.Code,
                        Name = x.bm.Name,
                        Address1 = x.bm.Address1,
                        Address2 = x.bm.Address2,
                        PostCode = x.bm.PostCode,
                        City = x.bm.City,
                        State = x.bm.State,
                        Phone = x.bm.Phone,
                        Fax = x.bm.Fax,
                        BankName = x.bm.BankName,
                        BankBranch = x.bm.BankBranch,
                        BankAccount = x.bm.BankAccount,
                        PersonIncharge = x.bm.PersonIncharge,
                        Email = x.bm.Email,
                        Description = x.bm.Description,
                        ShortName = x.bm.ShortName,
                        IsHeadQuarters = x.bm.IsHeadQuarters,
                        UbsCode = x.bm.UbsCode,
                        LastUpdate = x.bm.LastUpdate,
                        LastUpdatedBy = x.bm.LastUpdatedBy,
                        ParentBranch = x.bm.ParentBranch
                    })
                    .Distinct()
                    .ToListAsync();
            }

            if (isSuperAdmin)
            {
                // SuperAdmin → all banks
                bankList = await _oBMSDbContext.BankLists.Distinct().OrderBy(b=>b.BankName)
                    .ToListAsync();
            }
            else
            {
                // Normal user → join with OBMSBanks by BankID
                bankList = await _oBMSDbContext.BankLists
                    .Join(_oBMSDbContext.OBMSBanks,
                        bl => bl.ID,
                        ob => ob.BankID,
                        (bl, ob) => new { bl, ob })
                    .Where(x => x.ob.Name == userID && x.ob.IsAllowed == true)
                    .Select(x => new BankList
                    {
                        ID = x.bl.ID,
                        BankName = x.bl.BankName,
                        BankCode = x.bl.BankCode
                    })
                    .Distinct().OrderBy(b => b.BankName)
                    .ToListAsync();
            }

            var clientList = _oBMSDbContext.ClientMasters.Where(obj => obj.Status == "Active")?.Distinct().OrderBy(c => c.Name).ToList();

            var salaryStructureList = _oBMSDbContext.SalaryStructure?.ToList();



            results.Add("branchList", branchList);
            results.Add("clientList", clientList);
            results.Add("bankList", bankList);
            results.Add("stateList", Utility.Utility.GetStateList());
            results.Add("icColorList", Utility.Utility.GetICColorList());
            results.Add("nationalityList", Utility.Utility.GetNationalityList());
            results.Add("raceList", Utility.Utility.GetRaceList());
            results.Add("salaryStructureList", salaryStructureList);
            results.Add("emp", GetEmployeeNo());

            return results;
        }


        public async Task<List<Object>> GetClientsFromBranchId(string branchId)
        {
            var ret = _oBMSDbContext.ClientMasters.Where(x => x.Branch == branchId).Where(x => x.Status == "Active").ToList();
            return new List<Object>(ret);
        }

        public async Task<List<Object>> GetSalarySlabList(string employeeType, bool nonStructure)
        {

            var ret = _oBMSDbContext.SalaryStructure.Where(x => x.EmployeeType == employeeType && x.Status != null &&
                x.Status == "A" && (x.NonStructure == false || x.NonStructure == null));

            if (ret.Count() == 0)
            {
                ret = _oBMSDbContext.SalaryStructure.Where(x => x.EmployeeType == "GUARD" && x.Status != null &&
                x.Status == "A" && (x.NonStructure == false || x.NonStructure == null));
            }

            return new List<Object>(ret);
        }

        public async Task<Employee> saveAndUpdateEmployee(Employee employee, EmploymentDetails employment, EmployeeSalaryDetails salaryDetails)
        {


            if (employee.EMP_ID == 0)
            {
                _oBMSDbContext.Employees.Add(employee);
            }
            else
            {
                _oBMSDbContext.Employees.Update(employee);
            }

            await _oBMSDbContext.SaveChangesAsync();
            //return employee;



            employment.EMPPAY_CODE = employee.EMP_CODE;

            var _employment = await saveAndUpdateEmploymentDetails(employment);


            salaryDetails.EMPFL_CODE = employee.EMP_CODE;

            var _salaryDetails = await saveAndUpdateEmployeeSalaryDetails(salaryDetails);

            return employee;

            throw new NotImplementedException();
        }

        public async Task<EmploymentDetails> saveAndUpdateEmploymentDetails(EmploymentDetails employment)
        {
            if (employment.EMPPAY_ID == 0)
            {
                _oBMSDbContext.EmploymentDetails.Add(employment);
            }
            else
            {
                _oBMSDbContext.EmploymentDetails.Update(employment);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return employment;
            throw new NotImplementedException();
        }

        public async Task<EmployeeSalaryDetails> saveAndUpdateEmployeeSalaryDetails(EmployeeSalaryDetails salaryDetails)
        {
            if (salaryDetails.EMPFL_ID == 0)
            {
                _oBMSDbContext.EmployeeSalaryDetails.Add(salaryDetails);
            }
            else
            {
                _oBMSDbContext.EmployeeSalaryDetails.Update(salaryDetails);
            }

            await _oBMSDbContext.SaveChangesAsync();

            return salaryDetails;
            throw new NotImplementedException();
        }

        public async Task<Employee> UpdateEmployeeTransfer(EmployeeTransferDto employeeTransferDto)
        {
            var localBranchCode = "";
            var employee = _oBMSDbContext.Employees.Where(x => x.EMP_ID == employeeTransferDto.EMP_ID).FirstOrDefault();

            localBranchCode = employee.EMP_BRANCH_CODE;
            employee.EMP_BRANCH_CODE = employeeTransferDto.TO_BRANCH_ID;
            employee.OldBranch = localBranchCode;
            employee.HasTransfered = true;
            employee.TransferDate = employeeTransferDto.TRANSFER_DATE;

            var employment = _oBMSDbContext.EmploymentDetails.Where(x => x.EMPPAY_CODE == employee.EMP_CODE).FirstOrDefault();

            var salaryDetails = _oBMSDbContext.EmployeeSalaryDetails.Where(x => x.EMPFL_CODE == employee.EMP_CODE).FirstOrDefault();

            _oBMSDbContext.Employees.Update(employee);
            await _oBMSDbContext.SaveChangesAsync();
            var employeeHistory = new EmployeeHistory()
            {

                EMP_HISTORY_ID = 0,
                EMP_ID = employee.EMP_ID,
                EMP_ROLE = employee.EMP_ROLE,
                EMP_CODE = employee.EMP_CODE,
                EMP_NAME = employee.EMP_NAME,
                EMP_ADDRESS1 = employee.EMP_ADDRESS1,
                EMP_ADDRESS2 = employee.EMP_ADDRESS2,
                EMP_POST_CODE = employee.EMP_POST_CODE,
                EMP_TOWN = employee.EMP_TOWN,
                EMP_STATE = employee.EMP_STATE,
                EMP_NATIONAL = employee.EMP_NATIONAL,
                EMP_PHONE = employee.EMP_PHONE,
                EMP_HGH_EDU = employee.EMP_HGH_EDU,
                EM_WORK_EXP = employee.EM_WORK_EXP,
                EMP_DATE_OF_BIRTH = employee.EMP_DATE_OF_BIRTH,
                EMP_IC_OLD = employee.EMP_IC_OLD,
                EMP_IC_NEW = employee.EMP_IC_NEW,
                EMP_IC_COLOR = employee.EMP_IC_COLOR,
                EMP_PASSPORT_NO = employee.EMP_PASSPORT_NO,
                EMP_SEX = employee.EMP_SEX,
                EMP_RACE = employee.EMP_RACE,
                EMP_MARTIAL_STATUS = employee.EMP_MARTIAL_STATUS,
                EMP_SPOUSE_NAME = employee.EMP_SPOUSE_NAME,
                EMP_SP_IC = employee.EMP_SP_IC,
                EMP_NO_CHILD = employee.EMP_NO_CHILD,
                EMP_SP_WORK = employee.EMP_SP_WORK,
                EMP_PER_NAME_CONTACT = employee.EMP_PER_NAME_CONTACT,
                EMP_CONTACT_ADDRESS1 = employee.EMP_CONTACT_ADDRESS1,
                EMP_CONTACT_ADDRESS2 = employee.EMP_CONTACT_ADDRESS2,
                EMP_CONTACT_POST_CODE = employee.EMP_CONTACT_POST_CODE,
                EMP_CONTACT_TOWN = employee.EMP_CONTACT_TOWN,
                EMP_CONTACT_STATE = employee.EMP_CONTACT_STATE,
                EMP_CONTACT_TELEPHONE = employee.EMP_CONTACT_TELEPHONE,
                EMP_BRANCH_CODE = employeeTransferDto.TO_BRANCH_ID,
                OldBranch = localBranchCode,
                TransferDate = null,
                HasTransfered = false,
                LASTUPDATE = employee.LASTUPDATE,
                LastUpdatedBy = employee.LastUpdatedBy ?? "",
                EMP_MOBILEPHONE = employee.EMP_MOBILEPHONE,
                EMP_CITIZEN = employee.EMP_CITIZEN,
                EMP_CHECKLIST = employee.EMP_CHECKLIST,
                EMP_CLIENT = employee.EMP_CLIENT,
                NewSalaryStructure = employee.NewSalaryStructure,
                KDNVetting = employee.KDNVetting,
                SalaryStructure1000_3h = employee.SalaryStructure1000_3h,

                EMPPAY_JOB_TITLE = employment?.EMPPAY_JOB_TITLE ?? "",
                EMPPAY_CATEGORY = employment?.EMPPAY_CATEGORY ?? "",
                EMPPAY_DATE_JOINED = employment?.EMPPAY_DATE_JOINED,
                EMPPAY_DATE_CONFIRM = employment?.EMPPAY_DATE_CONFIRM,
                EMPPAY_DATE_RESIGNED = employment?.EMPPAY_DATE_RESIGNED,
                EMPPAY_BASIC_RATE = employment?.EMPPAY_BASIC_RATE ?? 0.0,
                SALARYLAB = employment?.SALARYLAB ?? 0,
                ATTENDANCEALLOWANCE = employment?.ATTENDANCEALLOWANCE,
                NewStructureATTENDANCEALLOWANCE = 0,
                SpecialAllowance = employment?.SpecialAllowance ?? 0,
                AttendanceAllowanceWorkingDays = employment?.AttendanceAllowanceWorkingDays,
                AttendanceAllowanceFollowCalendar = employment?.AttendanceAllowanceFollowCalendar,

                EMPFL_BANK = salaryDetails?.EMPFL_BANK ?? "",
                EMPFL_BK_ACCNO = salaryDetails?.EMPFL_BK_ACCNO ?? "",
                EMPFL_TAX_NO = salaryDetails?.EMPFL_TAX_NO,
                EMPFL_EPFNO = salaryDetails?.EMPFL_EPFNO,
                EMPFL_EPF8Pa = salaryDetails?.EMPFL_EPF8Pa,
                EMPFL_SOSCO_NO = salaryDetails?.EMPFL_SOSCO_NO,
                EPFDETECT = salaryDetails?.EPFDETECT ?? false,
                PAYMODE = salaryDetails?.PAYMODE ?? "",
                SOCSODETECT = salaryDetails?.SOCSODETECT ?? false,
                TMPGUARD = salaryDetails?.TMPGUARD ?? false,
                DETECTBYND55 = salaryDetails?.DETECTBYND55 ?? false,
                INCOMETAXDETECT = salaryDetails?.INCOMETAXDETECT,

            };

            _oBMSDbContext.EmployeeHistories.Add(employeeHistory);
            await _oBMSDbContext.SaveChangesAsync();

            return employee;

            throw new NotImplementedException();
        }

        #endregion


        //public async Task<Object> CheckEmployeeInfo(string from, string data)
        //{

        //    var results = new Dictionary<string, Object>();

        //    if (from == "NewIC")
        //    {


        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.Employee.EMP_IC_NEW == data &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED != null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();

        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE",employeeId ==0 ? "success": "Employee with the same New IC already exists in the database.");
        //                        return results;

        //    }else if( from == "OldIC")
        //    {

        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.Employee.EMP_IC_OLD == data &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();

        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same Old IC already exists in the database.");
        //                        return results;
        //    }else if(from == "Passport")
        //    {
        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.Employee.EMP_PASSPORT_NO == data &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();
        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same Passport already exists in the database.");
        //                        return results;
        //    }else if(from == "EPFNo")
        //    {
        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.SalaryDetails.EMPFL_EPFNO == data &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();
        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same EPF No already exists in the database.");
        //                        return results;
        //    }else if(from == "SOCSONo")
        //    {
        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.SalaryDetails.EMPFL_SOSCO_NO == data &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();

        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same SOCSO No already exists in the database.");
        //                        return results;
        //    }else if(from == "BankAccount")
        //    {

        //        string[] bank = data.Split(new string[] { "//" }, StringSplitOptions.None);

        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.SalaryDetails.EMPFL_BANK == bank[0] &&
        //                          result.Data.SalaryDetails.EMPFL_BK_ACCNO == bank[1] &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();


        //                        results.Add("EMP_ID", employeeId);
        //                        results.Add("FROM", from);
        //                        results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same Bank Account already exists in the database.");
        //                        return results;
        //    }
        //    else if (from == "BankAccount2")
        //    {

        //        string[] bank = data.Split(new string[] { "//" }, StringSplitOptions.None);

        //        var employeeId = _oBMSDbContext.Employees
        //            .Join(
        //                _oBMSDbContext.EmployeeSalaryDetails,
        //                employee => employee.EMP_CODE,
        //                salaryDetails => salaryDetails.EMPFL_CODE,
        //                (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails }
        //            )
        //            .Join(
        //                _oBMSDbContext.EmploymentDetails,
        //                data => data.SalaryDetails.EMPFL_CODE,
        //                employmentDetails => employmentDetails.EMPPAY_CODE,
        //                (data, employmentDetails) => new { Data = data, EmploymentDetails = employmentDetails }
        //            )
        //            .Where(
        //                result => result.Data.SalaryDetails.EMPFL_2ndBank == bank[0] &&
        //                          result.Data.SalaryDetails.EMPFL_2ndBK_ACCNO == bank[1] &&
        //                          result.Data.Employee.HasTransfered == false &&
        //                          result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null
        //            )
        //            .Select(result => result.Data.Employee.EMP_ID)
        //            .FirstOrDefault();


        //            results.Add("EMP_ID", employeeId);
        //            results.Add("FROM", from);
        //            results.Add("MESSAGE", employeeId == 0 ? "success" : "Employee with the same Bank Account 2 already exists in the database.");
        //            return results;
        //    }

        //    return null;

        //}

        public async Task<Object> CheckEmployeeInfo(string from, string data)
        {
            var today = DateTime.Today; // ✅ Added
            var results = new Dictionary<string, Object>();

            if (from == "NewIC")
            {
                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.Employee.EMP_IC_NEW == data &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same New IC already exists in the database.");
                return results;
            }
            else if (from == "OldIC")
            {
                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.Employee.EMP_IC_OLD == data &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same Old IC already exists in the database.");
                return results;
            }
            else if (from == "Passport")
            {
                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.Employee.EMP_PASSPORT_NO == data &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same Passport already exists in the database.");
                return results;
            }
            else if (from == "EPFNo")
            {
                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.SalaryDetails.EMPFL_EPFNO == data &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same EPF No already exists in the database.");
                return results;
            }
            else if (from == "SOCSONo")
            {
                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.SalaryDetails.EMPFL_SOSCO_NO == data &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same SOCSO No already exists in the database.");
                return results;
            }
            else if (from == "BankAccount")
            {
                string[] bank = data.Split(new string[] { "//" }, StringSplitOptions.None);

                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.SalaryDetails.EMPFL_BANK == bank[0] &&
                        result.Data.SalaryDetails.EMPFL_BK_ACCNO == bank[1] &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same Bank Account already exists in the database.");
                return results;
            }
            else if (from == "BankAccount2")
            {
                string[] bank = data.Split(new string[] { "//" }, StringSplitOptions.None);

                var employeeId = _oBMSDbContext.Employees
                    .Join(_oBMSDbContext.EmployeeSalaryDetails,
                        employee => employee.EMP_CODE,
                        salaryDetails => salaryDetails.EMPFL_CODE,
                        (employee, salaryDetails) => new { Employee = employee, SalaryDetails = salaryDetails })
                    .Join(_oBMSDbContext.EmploymentDetails,
                        d => d.SalaryDetails.EMPFL_CODE,
                        employmentDetails => employmentDetails.EMPPAY_CODE,
                        (d, employmentDetails) => new { Data = d, EmploymentDetails = employmentDetails })
                    .Where(result =>
                        result.Data.SalaryDetails.EMPFL_2ndBank == bank[0] &&
                        result.Data.SalaryDetails.EMPFL_2ndBK_ACCNO == bank[1] &&
                        result.Data.Employee.HasTransfered == false &&
                        (result.EmploymentDetails.EMPPAY_DATE_RESIGNED == null ||
                         result.EmploymentDetails.EMPPAY_DATE_RESIGNED > today)) // ✅ Updated
                    .Select(result => result.Data.Employee.EMP_ID)
                    .FirstOrDefault();

                results.Add("EMP_ID", employeeId);
                results.Add("FROM", from);
                results.Add("MESSAGE", employeeId == 0 ? "success"
                    : "Employee with the same Bank Account 2 already exists in the database.");
                return results;
            }

            return null;
        }

        public async Task<List<EmployeeHistoryDto>> GetAllEmployeesWithHistory(string? branch = null)
        {
            var query = from h in _oBMSDbContext.EmployeeHistories
                        join e in _oBMSDbContext.Employees on h.EMP_ID equals e.EMP_ID
                        select new EmployeeHistoryDto
                        {
                            EMP_ID = e.EMP_ID,
                            EMP_NAME = e.EMP_NAME,
                            EMP_CODE = e.EMP_CODE,
                            HasTransfered = e.HasTransfered,
                            TransferDate = e.TransferDate,
                            EMP_HISTORY_ID = h.EMP_HISTORY_ID,
                            EMP_ROLE = h.EMP_ROLE,
                            EMPPAY_DATE_JOINED = h.EMPPAY_DATE_JOINED,
                            EMPPAY_DATE_RESIGNED = h.EMPPAY_DATE_RESIGNED,
                            EMP_BRANCH_CODE = h.EMP_BRANCH_CODE,
                            OldBranch = h.OldBranch
                        };

            if (!string.IsNullOrEmpty(branch))
            {
                query = query.Where(x => x.EMP_BRANCH_CODE == branch);
            }

            return await query.ToListAsync();
        }
        public async Task<bool> DeleteEmployee(int employeeId)
        {
            var employee = await _oBMSDbContext.Employees
                .FirstOrDefaultAsync(e => e.EMP_ID == employeeId);

            if (employee == null)
                return false;

            _oBMSDbContext.Employees.Remove(employee);
            await _oBMSDbContext.SaveChangesAsync();

            return true;
        }

    }
}
[Keyless]
public class CheckEmployeeInfoResult
{
    public string? EMP_ID { get; set; }
}
