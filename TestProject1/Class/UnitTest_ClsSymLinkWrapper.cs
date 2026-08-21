using CmnClsLib.Class;
using FsFileUtil.Class;
using Xunit;

namespace TestProject1.Class
{
    public class UnitTest_ClsSymLinkWrapper
    {
        [Fact]
        public void Test_PropertiesAndInitialize()
        {
            var logger = new ClsLogger();
            var wrapper = new ClsSymLinkWrapper(logger)
            {
                Message = "Initial Message",
                RealPath = @"C:\TestPath",
                Verbose = 2,
                IsSilent = true
            };

            Assert.Equal("Initial Message", wrapper.Message);
            Assert.Equal(@"C:\TestPath", wrapper.RealPath);
            Assert.Equal(2, wrapper.Verbose);
            Assert.True(wrapper.IsSilent);

            wrapper.Initialize();
        }

        [Fact]
        public void Test_GetRealPathIfExists_InvalidPath()
        {
            var logger = new ClsLogger();
            var wrapper = new ClsSymLinkWrapper(logger)
            {
                Verbose = 3
            };

            string nonExistentPath = @"C:\NonExistentDirectory_12345\TestLink.lnk";
            string result = wrapper.GetRealPathIfExists(nonExistentPath, isRelative: false);

            Assert.Empty(result);
            Assert.Contains("NO SUCH A FILE OR DIRECTORY", wrapper.Message);
        }

        [Fact]
        public void Test_GetRealPathIfExists_NullOrEmptyPath()
        {
            var logger = new ClsLogger();
            var wrapper = new ClsSymLinkWrapper(logger);

            string resultEmpty = wrapper.GetRealPathIfExists(string.Empty, isRelative: false);
            Assert.Empty(resultEmpty);

            string resultWhitespace = wrapper.GetRealPathIfExists("   ", isRelative: false);
            Assert.Empty(resultWhitespace);
        }

        [Fact]
        public void Test_WriteLine_SilentMode()
        {
            var logger = new ClsLogger();
            var wrapper = new ClsSymLinkWrapper(logger)
            {
                IsSilent = true
            };

            wrapper.WriteLine(0, "Test Silent Message");
        }
    }
}
