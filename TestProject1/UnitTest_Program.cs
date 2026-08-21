using System;
using CmnClsLib.Class;
using CmnClsLib.Module;
using FsFileUtil;
using Xunit;

namespace TestProject1
{
    public class UnitTest_Program
    {
        [Fact]
        public void Main_WithUsageOnlyArgument_ReturnsErrorExitCode()
        {
            // Arrange (-? only results in Parse Args returning false, setting exit code to LVL_E)
            string[] args = ["-?"];

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.Equal(MdlConst.LVL_E, exitCode);
        }

        [Fact]
        public void Main_WithHelpOnlyArgument_ReturnsErrorExitCode()
        {
            // Arrange (-h only results in Parse Args returning false, setting exit code to LVL_E)
            string[] args = ["-h"];

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.Equal(MdlConst.LVL_E, exitCode);
        }

        [Fact]
        public void Main_WithValidActionAndHelp_ReturnsWarningExitCode()
        {
            // Arrange (Valid action and required parameters with usage flag causes Parse Args to succeed and return LVL_W)
            string[] args = ["-act", "copy", "-f", @"C:\test.txt", "-t", @"C:\dst.txt", "-?"];

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.Equal(MdlConst.LVL_W, exitCode);
        }

        [Fact]
        public void Main_WithInvalidArgument_ReturnsErrorExitCode()
        {
            // Arrange
            string[] args = ["--invalid-argument-test"];

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.Equal(MdlConst.LVL_E, exitCode);
        }

        [Fact]
        public void Main_WithNoActionAndNoArgs_ReturnsErrorExitCode()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act
            int exitCode = Program.Main(args);

            // Assert
            Assert.Equal(MdlConst.LVL_E, exitCode);
        }
    }
}
