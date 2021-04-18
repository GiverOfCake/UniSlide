namespace RhythmEngine.Model
{
    public class LanePosition
    {
        /// <summary>
        /// The leftmost starting lane (0-31)
        /// </summary>
        public int Lane;
        
        /// <summary>
        /// The distance between the leftmost and rightmost lane this object occupies (1-32)
        /// </summary>
        public int Width;

        public LanePosition(int lane, int width)
        {
            Lane = lane;
            Width = width;
        }

        public float Center => Lane + Width / 2f;
    }
}