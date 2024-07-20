using Xunit;

namespace Fakemail.Core.Tests
{
    public class Base62Tests
    {
        [Fact]
        public void LengthOfIdShouldBeCorrect()
        {
            Assert.Equal(22, Utils.CreateId().Length);
            Assert.Equal(22, Utils.CreateId(16).Length);
            Assert.Equal(21, Utils.CreateId(15).Length);
            Assert.Equal(19, Utils.CreateId(14).Length);
            Assert.Equal(18, Utils.CreateId(13).Length);
            Assert.Equal(17, Utils.CreateId(12).Length);
            Assert.Equal(15, Utils.CreateId(11).Length);
            Assert.Equal(14, Utils.CreateId(10).Length);
            Assert.Equal(13, Utils.CreateId(9).Length);
            Assert.Equal(11, Utils.CreateId(8).Length);
            Assert.Equal(10, Utils.CreateId(7).Length);
            Assert.Equal(9, Utils.CreateId(6).Length);
            Assert.Equal(7, Utils.CreateId(5).Length);
            Assert.Equal(6, Utils.CreateId(4).Length);
            Assert.Equal(5, Utils.CreateId(3).Length);
            Assert.Equal(3, Utils.CreateId(2).Length);
            Assert.Equal(2, Utils.CreateId(1).Length);
        }
    }
}