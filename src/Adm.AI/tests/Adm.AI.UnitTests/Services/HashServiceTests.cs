using Adm.AI.Application.Services;
using System.Text;
namespace Adm.AI.UnitTests.Services;
public class HashServiceTests
{
    private readonly HashService _sut;
    public HashServiceTests()
    {
        _sut = new HashService();
    }
    [Fact]
    public void ComputeHash_EmptyArray_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ComputeHash(Array.Empty<byte>());
        // Assert
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public void ComputeHash_NullArray_ReturnsEmptyString()
    {
        // Act
        var result = _sut.ComputeHash(null!);
        // Assert
        Assert.Equal(string.Empty, result);
    }
    [Fact]
    public void ComputeHash_ValidData_ReturnsDeterministicSha256Hex()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("hello world");
        // echo -n "hello world" | shasum -a 256 -> b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9
        var expectedHash = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";
        // Act
        var result = _sut.ComputeHash(data);
        // Assert
        Assert.Equal(expectedHash, result);
    }
    [Fact]
    public void ComputeHash_DifferentData_ReturnsDifferentHashes()
    {
        // Arrange
        var data1 = Encoding.UTF8.GetBytes("file1");
        var data2 = Encoding.UTF8.GetBytes("file2");
        // Act
        var result1 = _sut.ComputeHash(data1);
        var result2 = _sut.ComputeHash(data2);
        // Assert
        Assert.NotEqual(result1, result2);
    }
}