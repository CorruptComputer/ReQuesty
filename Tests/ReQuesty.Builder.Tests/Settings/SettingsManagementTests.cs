using Microsoft.OpenApi;
using Xunit;

namespace ReQuesty.Builder.Settings.Tests
{
    public class SettingsFileManagementServiceTest
    {
        [Fact]
        public void GetDirectoryContainingSettingsFile_ShouldCreateTheDirectory_If_It_Doesnt_Exist()
        {
            // Arrange
            SettingsFileManagementService service = new();
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            // Act
            string result = service.GetDirectoryContainingSettingsFile(tempDirectory);
            tempDirectory = Path.Combine(tempDirectory, ".vscode");
            // Assert
            Assert.Equal(tempDirectory, result);

            // Cleanup
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch (IOException)
            {
                // Handle the case where the directory is not empty
                Directory.Delete(tempDirectory, true);
            }
        }

        [Fact]
        public void GetDirectoryContainingSettingsFile_ShouldReturnVscodeDirectory_WhenExists()
        {
            // Arrange
            SettingsFileManagementService service = new();
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string vscodeDirectory = Path.Combine(tempDirectory, ".vscode");
            Directory.CreateDirectory(vscodeDirectory);

            // Act
            string result = service.GetDirectoryContainingSettingsFile(tempDirectory);

            // Assert
            Assert.Equal(vscodeDirectory, result);

            // Cleanup
            Directory.Delete(tempDirectory, true);
        }

        [Fact]
        public async Task WriteSettingsFileAsync_ShouldThrowArgumentException_WhenDirectoryPathIsNullOrEmpty()
        {
            // Arrange
            SettingsFileManagementService service = new();
            OpenApiDocument openApiDocument = new();
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.WriteSettingsFileAsync(string.Empty, openApiDocument, cancellationToken));
        }

        [Fact]
        public async Task WriteSettingsFileAsync_ShouldThrowArgumentNullException_WhenOpenApiDocumentIsNull()
        {
            // Arrange
            SettingsFileManagementService service = new();
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            CancellationToken cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.WriteSettingsFileAsync(tempDirectory, null!, cancellationToken));

            // Cleanup
            Directory.Delete(tempDirectory);
        }
    }
}
