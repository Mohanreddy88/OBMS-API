using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface IEmployeeRepository
    {
        Task<Dictionary<string, string>> GetEmployeeNoByBranchID(string branchId);
        Task<Dictionary<string, string>> GetEmployeeNo();
        Task<Dictionary<string, Object>> GetEmployeeMasterList(string userID);
        Task<List<Object>> GetClientsFromBranchId(string branchId);
        Task<List<Object>> GetSalarySlabList( string employeeType, bool nonStructure);
        Task<Employee> saveAndUpdateEmployee(Employee employee, EmploymentDetails employment, EmployeeSalaryDetails salaryDetails);
        Task<Employee> UpdateEmployeeTransfer(EmployeeTransferDto employeeTransferDto);
        Task<Dictionary<string, Object>> GetEmployeeById(int employeeId);
        Task<Object> CheckEmployeeInfo(string from,string data);
        Task<List<EmployeeHistoryDto>> GetAllEmployeesWithHistory(string? branch = null);
        Task<bool> DeleteEmployee(int employeeId);

    }
}
