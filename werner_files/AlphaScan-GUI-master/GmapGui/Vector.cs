
namespace ParkDS
{
    /// <summary>
    /// Two dimensional vector used for mathematical operations.
    /// </summary>
    public class Vector
    {
        /// <summary>
        /// First value.
        /// </summary>
        public double X;
        /// <summary>
        /// Second value.
        /// </summary>
        public double Y;

        // Constructors.
        public Vector(double x, double y) { X = x; Y = y; }
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Vector() : this(double.NaN, double.NaN) { }

        /// <summary>
        /// Vector subtraction operator.
        /// </summary>
        /// <param name="v">First term.</param>
        /// <param name="w">Second term. Subtracted from <paramref name="v"/>.</param>
        /// <returns>The vector difference between the two operands.</returns>
        public static Vector operator -(Vector v, Vector w)
        {
            return new Vector(v.X - w.X, v.Y - w.Y);
        }

        public static Vector operator +(Vector v, Vector w)
        {
            return new Vector(v.X + w.X, v.Y + w.Y);
        }

        /// <summary>
        /// Dot product operator.
        /// </summary>
        /// <param name="v">First term.</param>
        /// <param name="w">Second term.</param>
        /// <returns>Dot product of <paramref name="v"/> and <paramref name="w"/>.</returns>
        public static double operator *(Vector v, Vector w)
        {
            return v.X * w.X + v.Y * w.Y;
        }

        /// <summary>
        /// Scalar multiplication operator.
        /// </summary>
        /// <param name="v">Vector term.</param>
        /// <param name="mult">Scalar term.</param>
        /// <returns>Vector <paramref name="v"/> multiplied by scalar <paramref name="mult"/>.</returns>
        public static Vector operator *(Vector v, double mult)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        /// <summary>
        /// Scalar multiplication operator.
        /// </summary>
        /// <param name="v">Vector term.</param>
        /// <param name="mult">Scalar term.</param>
        /// <returns>Vector <paramref name="v"/> multiplied by scalar <paramref name="mult"/>.</returns>
        public static Vector operator *(double mult, Vector v)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        /// <summary>
        /// Cross product of this vector and <paramref name="v"/>. Eg. (this) cross (<paramref name="v"/>)
        /// </summary>
        /// <param name="v">Second term in the cross product.</param>
        /// <returns>Cross product of this vector and <paramref name="v"/>.</returns>
        public double Cross(Vector v)
        {
            return X * v.Y - Y * v.X;
        }

        /// <summary>
        /// Determines if two vectors are equivalent.
        /// </summary>
        /// <param name="v">Vector to compare against the current vector.</param>
        /// <returns>Whether the two vectors have equivalent terms.</returns>
        public bool Equals(Vector v)
        {
            return (X - v.X).IsZero() && (Y - v.Y).IsZero();
        }
    }
}
