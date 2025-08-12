using ReQuesty.Builder.CodeDOM;
using ReQuesty.Builder.PathSegmenters;
using Xunit;

namespace ReQuesty.Builder.Tests.PathSegmenters
{
    public class CSharpPathSegmenterTests
    {
        private readonly CSharpPathSegmenter segmenter;
        public CSharpPathSegmenterTests()
        {
            segmenter = new CSharpPathSegmenter("D:\\source\\repos\\requesty-sample", "client");
        }

        [Fact]
        public void CSharpPathSegmenterGeneratesCorrectFileName()
        {
            string fileName = segmenter.NormalizeFileName(new CodeClass
            {
                Name = "testClass"
            });
            Assert.Equal("TestClass", fileName);// the file name should be PascalCase
        }

        [Fact]
        public void CSharpPathSegmenterDoesNotGenerateNamespaceFoldersThatAreTooLong()
        {
            string longNamespace = "ThisIsAVeryLongNamespaceNameThatShouldBeTruncatedAsItIsLongerThanTwoHundredAndFiftySixCharactersAccordingToWindows";
            longNamespace = longNamespace + longNamespace + longNamespace; //make it more than 256
            string normalizedNamespace = segmenter.NormalizeNamespaceSegment(longNamespace);
            Assert.NotEqual(longNamespace, normalizedNamespace);
            Assert.Equal(64, normalizedNamespace.Length);// shortened to sha256 length
        }

        [Fact]
        public void CSharpPathSegmenterDoesNotGeneratePathsThatAreTooLong()
        {
            string rootDir = "D:\\source\\repos\\requesty-sample\\Item\\ErpBOLaborSvc\\";
            string longNamespace = "ThisIsAVeryLongNamespaceNameThatIsNotLongerThanTwoHundredAndFiftySixCharactersAccordingToWindows";
            while (rootDir.Length < CSharpPathSegmenter.MaxFilePathLength - longNamespace.Length)
            {
                rootDir = $"{rootDir}\\{longNamespace}";
            }
            string longPathName = $"{rootDir}\\TimeWeeklyViewsWithCompanyWithEmployeeNumWithWeekBeginDateWithWeekEndDateWithQuickEntryCodeWithProjectIDWithPhaseIDWithTimeTypCdWithJobNumWithAssemblySeqWithOprSeqWithRoleCdWithIndirectCodeWithExpenseCodeWithResourceGrpIDWithResourceIDWithLaborTypePseudoWithShiftWithNewRowTypeWithTimeStatusRequestBuilder.cs";
            string normalizedPath = segmenter.NormalizePath(longPathName);
            Assert.NotEqual(longPathName, normalizedPath);
            Assert.True(normalizedPath.Length < longPathName.Length);//new path is shorter than the original
            Assert.True(normalizedPath.Length < CSharpPathSegmenter.MaxFilePathLength); // new path is shorter than the max path length
        }
    }
}
