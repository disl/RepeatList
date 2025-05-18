using RepeatList.Models;

namespace RepeatList.Services
{
    public class PositionComparer : IEqualityComparer<Position>
    {
        public bool Equals(Position x, Position y)
        {
            return x.Id == y.Id && x.Title == y.Title;
        }

        public int GetHashCode(Position obj)
        {
            return HashCode.Combine(obj.Id, obj.Title);
        }
    }

}
