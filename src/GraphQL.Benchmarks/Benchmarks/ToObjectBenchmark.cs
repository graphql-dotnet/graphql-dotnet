#nullable enable

using BenchmarkDotNet.Attributes;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Benchmarks;

[MemoryDiagnoser]
public class ToObjectBenchmark : IBenchmark
{
    private IInputObjectGraphType _personType = null!;
    private IInputObjectGraphType _companyType = null!;
    private IInputObjectGraphType _employeeType = null!;
    private IInputObjectGraphType _salaryType = null!;
    private Dictionary<string, object?> _personData = null!;
    private readonly Dictionary<string, object?> _noData = [];
    private Dictionary<string, object?> _companyData = null!;
    private Dictionary<string, object?> _employeeData = null!;
    private Dictionary<string, object?> _salaryData = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>());

        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        _personType = (IInputObjectGraphType)schema.AllTypes[nameof(Person)]!;
        _companyType = (IInputObjectGraphType)schema.AllTypes[nameof(Company)]!;
        _employeeType = (IInputObjectGraphType)schema.AllTypes[nameof(Employee)]!;
        _salaryType = (IInputObjectGraphType)schema.AllTypes[nameof(SalaryInfo)]!;
        _personData = new Dictionary<string, object?>()
        {
            { "name", "John Doe" },
            { "age", 123 }
        };

        _companyData = new Dictionary<string, object?>()
        {
            { "name", "John Doe" },
            { "emailAddress", "johndoe@example.dummy" },
            { "phoneNumber", "123-456-7890" },
            { "website", new Uri("https://github.com/graphql-dotnet/graphql-dotnet") },
            { "employees", new List<object?>()
                {
                    new Employee()
                    {
                        FirstName = null!,
                        LastName = null!,
                        SalaryHistory = null!,
                    },
                    new Employee()
                    {
                        FirstName = null!,
                        LastName = null!,
                        SalaryHistory = null!,
                    },
                }
            },
            { "addresses", new object?[] { } }
        };

        _employeeData = new Dictionary<string, object?>
        {
            { "firstName", "John" },
            { "lastName", "Doe" },
            { "emailAddress", "john.doe@example.com" },
            { "salaryHistory", new object[] {} },
            { "address1", "123 Main St" },
            { "address2", "Apt 4" },
            { "city", "Anytown" },
            { "state", "Anystate" },
            { "zipCode", "12345" },
            { "phoneNumber", "123-456-7890" }
        };

        _salaryData = new Dictionary<string, object?>
        {
            { "income", 50000.0 },
            { "salary", 45000.0 },
            { "bonus", 5000.0 },
            { "benefits", 7000.0 },
            { "taxes", 10000.0 }
        };
    }

    [Benchmark]
    public void Person_Populated()
    {
        _ = (Person)_personType.ParseDictionary(_personData);
    }

    [Benchmark]
    public void Person_Undefined()
    {
        _ = (Person)_personType.ParseDictionary(_noData);
    }

    [Benchmark]
    public void Company_Populated()
    {
        _ = (Company)_companyType.ParseDictionary(_companyData);
    }

    [Benchmark]
    public void Company_Undefined()
    {
        _ = (Company)_companyType.ParseDictionary(_noData);
    }

    [Benchmark]
    public void Employee_Populated()
    {
        _ = (Employee)_employeeType.ParseDictionary(_employeeData);
    }

    [Benchmark]
    public void Employee_Undefined()
    {
        _ = (Employee)_employeeType.ParseDictionary(_noData);
    }

    [Benchmark]
    public void Salary_Populated()
    {
        _ = (SalaryInfo)_salaryType.ParseDictionary(_salaryData);
    }

    [Benchmark]
    public void Salary_Undefined()
    {
        _ = (SalaryInfo)_salaryType.ParseDictionary(_noData);
    }

    void IBenchmark.RunProfiler() => Person_Populated();

    public class Query
    {
        public virtual string TestClass1(Person arg) => "ok";
        public virtual string TestClass2(Company arg) => "ok";
    }

    public class Person
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    }

    public class Employee : Address
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? EmailAddress { get; set; }
        public required SalaryInfo[] SalaryHistory { get; set; }
    }

    // sample class with a lot of value-type fields
    public class SalaryInfo
    {
        public double Income { get; set; }
        public double Salary { get; set; }
        public double Bonus { get; set; }
        public double Benefits { get; set; }
        public double Taxes { get; set; }
    }

    // sample class with a lot of class-type fields
    public class Address
    {
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CompanyAddress : Address
    {
        public required string Description { get; set; }
    }

    public class Company
    {
        public required string Name { get; set; }
        public required List<Employee> Employees { get; set; }
        public required List<CompanyAddress> Addresses { get; set; }
        public required string EmailAddress { get; set; }
        public required string PhoneNumber { get; set; }
        public Uri? Website { get; set; }
    }
}
