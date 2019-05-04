using NUnit.Framework;

namespace Tests
{
    public class HexCoordinatesTest
    {
        [Test]
        public void Y_Calculation_Test()
        {
            var c1 = new HexCoordinates(0, 0);
            var c2 = new HexCoordinates(4, 4);
            var c3 = new HexCoordinates(4, 2);

            Assert.AreEqual(0, c1.Y);
            Assert.AreEqual(-8, c2.Y);
            Assert.AreEqual(-6, c3.Y);
        }

        [Test]
        public void Distance_Different_Hexes_Test_1()
        {
            var c1 = new HexCoordinates(6, 3);
            var c2 = new HexCoordinates(2, 4);

            int dis = c1.DistanceTo(c2);

            Assert.AreEqual(4, dis);
        }

        [Test]
        public void Distance_Different_Hexes_Test_2()
        {
            var c1 = new HexCoordinates(1, 1);
            var c2 = new HexCoordinates(5, 3);

            int dis = c1.DistanceTo(c2);

            Assert.AreEqual(6, dis);
        }

        [Test]
        public void Distance_Same_Hexes_Test()
        {
            var c1 = new HexCoordinates(16, 6);
            var c2 = new HexCoordinates(16, 6);

            int dis = c1.DistanceTo(c2);

            Assert.AreEqual(0, dis);
        }
    }
}
