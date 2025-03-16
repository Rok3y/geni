namespace LaunchService.test
{
    public class UnitTest1
    {
        [Fact]
        public void Test_GetNextWeekRange1()
        {
            // Arrange
            DateTime testDate = new DateTime(2025, 3, 13); // Thursday, March 13, 2025

            // Act
            var (start, end) = Helper.GetNextWeekRange(testDate);

            // Assert 
            Assert.Equal(new DateTime(2025, 3, 17, 0, 0, 0), start);
            Assert.Equal(new DateTime(2025, 3, 23, 23, 59, 59), end);
        }

        [Fact]
        public void Test_GetNextWeekRange2()
        {
            // Arrange
            DateTime testDate = new DateTime(2025, 3, 10, 0, 0, 0); // Monday, March 10, 2025

            // Act
            var (start, end) = Helper.GetNextWeekRange(testDate);

            // Assert 
            Assert.Equal(new DateTime(2025, 3, 17, 0, 0, 0), start);
            Assert.Equal(new DateTime(2025, 3, 23, 23, 59, 59), end);
        }

        [Fact]
        public void Test_GetNextWeekRange3()
        {
            // Arrange
            DateTime testDate = new DateTime(2025, 3, 16, 23, 59, 59); // Sunday, March 16, 2025

            // Act
            var (start, end) = Helper.GetNextWeekRange(testDate);

            // Assert 
            Assert.Equal(new DateTime(2025, 3, 17, 0, 0, 0), start);
            Assert.Equal(new DateTime(2025, 3, 23, 23, 59, 59), end);
        }
    }
}