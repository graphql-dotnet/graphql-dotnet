#nullable disable
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace GraphQL.Tests.Reflection.NullableTests
{
    public class NullableClass21
    {
        public static string Field1() => "field1";
        [Required]
        public static string Field2() => "field2";
        [Optional]
        public static string Field3() => "field3";
        public static int Field4() => 1;
        [Optional]
        public static int Field5() => 1;
        public static int? Field6() => 1;
        [Required]
        public static int? Field7() => 1;
        [Id]
        public static string Field8() => "field8";
        [Id]
        [Required]
        public static string Field9() => "field9";
        public static List<string> Field10() => null;
        [Required]
        public static List<string> Field11() => null;
        [Optional]
        public static List<string> Field12() => null;
        [RequiredList]
        public static List<string> Field13() => null;
        [OptionalList]
        public static List<string> Field14() => null;
        [RequiredList, Required]
        public static List<string> Field15() => null;
        [RequiredList, Optional]
        public static List<string> Field16() => null;
        [OptionalList, Required]
        public static List<string> Field17() => null;
        [OptionalList, Optional]
        public static List<string> Field18() => null;
        public static IDataLoaderResult<string> Field19() => null;
        [Required]
        public static IDataLoaderResult<string> Field20() => null;
        public static Task<string> Field21() => null;
        [Required]
        public static Task<string> Field22() => null;
        public static List<int> Field23() => null;
        public static List<int?> Field24() => null;
    }
}
