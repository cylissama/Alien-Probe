using System;
using System.Numerics;
using DelaunatorSharp;
using DelaunatorSharp.Interfaces;
using DelaunatorSharp.Models;
using System.Collections.ObjectModel;

namespace CES.AlphaScan.mmWave
{
    public class AzimuthProcessing
    {
        /*
            Class for processing mmWave packets that contain azimuth heat data (TLV 4). These seteps involve creating a 200x200 grid that varies
            from an X position of -5 to 5 and a Y position of 0 to 11 meters from the prespective of the mmWave sensor. Delauney triangles are also
            used to interpolate strengths onto a grid point, even though the actual values and locations found by the sensor may be off or between different
            discrete XY values on the grid. an FFT is performed on the recieved packet to analyze the data and get into a form that can be analyzed. There are
            multiple matrix manipualtions that take place to cut out data that is unneeded and account for any possible area. Linear algebraic functionsa are then
            performed on the transformed data in the manipulated matricies to find the interpolated heat strength values on each coordinate point. It is important to note
            that data that has a low strength value with be interpreted as a NaN due to the exteremely small heat strength from the data. Data is then returned in its final,
            interpolated XY strength matrix to be used for clustering
        */

        #region variables
        int NUM_ANGLE_BINS = 64; int NUM_RANGE_BINS = 256;  // variables for constructing heatmap grid
        int numBytes = 8192;      // find number of bytes in heatmap

        // More static variables that are used for distance and point location of heatmap
        readonly double rangeIdxToMeters = 0.0436;
        static double[] theta = new double[63];
        static double[] range = new double[256];

        static double[,] X;
        static double[,] Y;

        // data of position values for the grid
        posStruct[] positionStruct;
        // XY points for use in Delauney triangulations
        static ObservableCollection<IPoint> points = new ObservableCollection<IPoint>();
        // check for initialization to prevent redundant data from beign produced
        static bool isInit = false;

        /// <summary>
        /// Struct of X Y grid data values (in meters)
        /// </summary>
        struct posStruct
        {
            public double X;
            public double Y;
        }
        #endregion

        /// <summary>
        /// processes packet of azimuth heatmap data 
        /// </summary>
        /// <param name="byteVecMatrix">input matrix to process</param>
        /// <param name="idx">index of packet within byteVecMatrix</param>
        /// <returns>interpolated mmWave data to cluster</returns>
        public InterpolationData[] ProcessAzimuthHeatMap(byte[] byteVecMatrix, int idx)
        {
            // Get slice of bytes and add to array
            byte[] byteSlice = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)  // get slice of bytes desired, 8192 for heatmap
            {
                byteSlice[i] = byteVecMatrix[idx + i];
            }

            int[] azimuthArray = new int[4096]; // array for little endian conversion values from heatmap

            // variables that need to be initialized during the first data packet but are static...
            int miscIdx = 0;
            if (!isInit)
            {
                // finds theta and range values for configuring the heatmap
                for (int i = -NUM_ANGLE_BINS / 2 + 1; i <= NUM_ANGLE_BINS / 2 - 1; i++)  // calculate theta values
                {
                    theta[miscIdx] = Math.Asin(i * (2.0 / NUM_ANGLE_BINS)) * (180 / Math.PI);
                    miscIdx += 1;
                }
                for (int i = 0; i < range.Length; i++)  // calculate range values
                {
                    range[i] = i * rangeIdxToMeters;
                }
                miscIdx = 0;

                // setting up an evenly spaced coordinate system basedo n range and length, will be a 200x200 matrix
                X = MeshGrid(LinSpace(-Math.Floor(range[range.Length - 1]), Math.Ceiling(range[range.Length - 1]), 200), 0);
                Y = MeshGrid(LinSpace(0, Math.Ceiling(range[range.Length - 1]), 200), 1);
                // find all position values on the meshgrid using the range and theta values
                positionStruct = FindPos(range, theta);

                // for points in xq yq, 200x200 matrix
                Point dummy = new Point();
                points.Clear();
                // adds all XY points to the 200x200 grid 
                for (int i = 0; i < X.GetLength(0); i++)
                {
                    for (int j = 0; j < Y.GetLength(1); j++)
                    {
                        dummy.X = X[j, i];
                        dummy.Y = Y[j, i];
                        points.Add(dummy);
                    }
                }
                // prevents the first time set up from running again
                isInit = true;
            }

            for (int i = 0; i < byteSlice.Length; i += 2)    // calculate values of strengths, make negative if so
            {
                azimuthArray[miscIdx] = byteSlice[i] + (byteSlice[i + 1] * 256);        // little endian conversion
                if (azimuthArray[miscIdx] > 32767)  // check to see if value is negative (imaginary) or positive
                {
                    azimuthArray[miscIdx] -= 65536; // value will become negativce, will eventually become an imaginary number
                }
                miscIdx += 1;
            }

            miscIdx = 0;
            Complex[] azimuthArrayImag = new Complex[2048]; // prepare a complex matrix for the FFT
            for (int i = 0; i < azimuthArray.Length; i += 2)    // make array of complex, imaginary numbers for fouier transform
            {
                azimuthArrayImag[miscIdx] = new Complex(azimuthArray[i + 1], azimuthArray[i]); // data is set in 2 parts for each value, imaginary, real, is flipped in this case
                miscIdx += 1;
            }

            // prepare for the reshape of the complex array
            int reshapeRow = 0;
            int reshapeCol = 0;
            Complex[,] azimuthArrayReshape = new Complex[8, 256];
            for (int i = 0; i < azimuthArrayImag.Length; i++)       // reshape azimuth array to fit fourier transform, 8x256 on default settings
            {
                azimuthArrayReshape[reshapeRow, reshapeCol] = azimuthArrayImag[i];
                reshapeRow += 1;

                if (reshapeRow == 8)
                {
                    reshapeRow = 0;
                    reshapeCol += 1;
                }
            }
            // fft transformation, convert the phase data into usable values
            double[,] transformArray = Fft(azimuthArrayReshape);

            transformArray = FftShift(transformArray);
            transformArray = TransposeMatrix(transformArray);
            transformArray = CutFirstCol(transformArray);

            InterpolationData[] finalStruct = InterpolateData(positionStruct, transformArray);

            return finalStruct;
        }
        #region mathFunctions

        Complex i = new Complex(0, 1);

        /// <summary>
        /// discrete fast fourier transform to convert data into usefuk informaiton
        /// </summary>
        /// <param name="input">complex array of data to transform</param>
        /// <param name="N">desired output dimensions</param>
        /// <returns></returns>
        private double[,] Fft(Complex[,] input)
        {
            // see get desired array size, these should be static
            double[,] transformArray = new double[NUM_ANGLE_BINS, NUM_RANGE_BINS];
            // indexing variables
            int k = 0;
            int row = 0;
            int col = 0;

            // performs fft, goes through all indexes of the inputted array, is default at 16384
            for (int index = 0; index < 16384; index++)
            {
                // go through a column at a time
                Complex sum = 0;
                Complex[] singleCol = new Complex[64];
                // go through the row on the column being observed
                for (int rowCount = 0; rowCount < 64; rowCount++)
                {
                    // find the complex exponential to get a sum from the rows oberseved on the column
                    if (rowCount < input.GetLength(0))
                    {
                        singleCol[rowCount] = input[rowCount, col];
                        sum += singleCol[rowCount] * Complex.Exp((-i * 2 * Math.PI * k * rowCount) * 0.015625); // this *0.015625 is a default value, probably wont change
                    }
                    else
                        singleCol[rowCount] = 0;
                }
                // find the magnitude to get the usable value strength for each point
                transformArray[row, col] = sum.Magnitude;
                ++col;
                // if at the end of the row, move to next and sweep
                if (col == 256)
                {
                    col = 0;
                    ++row;
                    k += 1;
                }
            }
            return transformArray;
        }

        /// <summary>
        /// shifts fft by dimension, only 1 is supported
        /// </summary>
        /// <param name="input">input data from fourier transform</param>
        /// <param name="dim">dimension to shift, use 1</param>
        /// <returns></returns>
        private double[,] FftShift(double[,] input)
        {
            double[,] shiftMatrix = new double[64, 256];    // shifted matrix output

            for (int col = 0; col < 256; col++) // go through column by column
            {
                for (int rowIndex = 0; rowIndex < 32; rowIndex++)   // search the row within the column being observed
                {
                    shiftMatrix[rowIndex, col] = input[rowIndex + 32, col]; // shift all the values in the row by 1
                    shiftMatrix[rowIndex + 32, col] = input[rowIndex, col];
                }
            }
            return shiftMatrix;
        }

        /// <summary>
        /// transposes the matrix to flip row and col
        /// </summary>
        /// <param name="input">matrix to flip</param>
        /// <returns>the inputted matrix with its rows and columns flipped</returns>
        private double[,] TransposeMatrix(double[,] input)
        {
            double[,] transposedMatrix = new double[256, 64];   // returned matrix to flip

            for (int row = 0; row < 64; row++)  // go through row by row
            {
                for (int col = 0; col < 256; col++) // search the column within the row being observed
                {
                    transposedMatrix[col, row] = input[row, col];   // flip the row and column location of the strength being observed
                }
            }
            return transposedMatrix;
        }

        /// <summary>
        /// cuts first column of a flipped matrix
        /// </summary>
        /// <param name="input">matrix to cut first column of</param>
        /// <returns>matrix with first column cut</returns>
        private double[,] CutFirstCol(double[,] input)
        {
            double[,] cutArray = new double[256, 63];   // returnd matrix with row cut
            for (int row = 0; row < 256; row++) // go through row by row
            {
                for (int col = 1; col < 64; col++)  // search the column within the row being observed
                {
                    cutArray[row, col - 1] = input[row, col];   // shift all of the values back by one column since the first column is being removed
                }
            }
            return cutArray;
        }

        /// <summary>
        /// performs linear spacing of data to get equal spacing in desired area
        /// </summary>
        /// <param name="x1">starting position</param>
        /// <param name="x2">finishing position</param>
        /// <param name="n">desired amount of locations between x1 and x2</param>
        /// <returns>a spaced area between x1 and x2 with n amount of points</returns>
        private double[] LinSpace(double x1, double x2, int n)
        {
            double[] spacedVector = new double[n];  // array for the evenly spaced vector
            double stepSize = (Math.Abs(x2) + Math.Abs(x1)) / (n - 1);  // calculated stepsize between each point on the vector
            for (int index = 0; index < n; index++)
            {
                spacedVector[index] = x1 + (stepSize * index);      // calculates the meter value of the step value's index
            }
            return spacedVector;
        }

        /// <summary>
        /// performs mesh grid calculation of data to return matrix
        /// </summary>
        /// <param name="input">linearlly spaced data to make a meshgrid from</param>
        /// <param name="type">coordinate type of meshfrid, 0 is X, 1 is Y</param>
        /// <returns></returns>
        private double[,] MeshGrid(double[] input, int type)
        {
            double[,] grid = new double[input.Length, input.Length];    // outputted mesh grid

            if (type == 0)  // if x coordinate
            {
                int xLength = input.Length; // go through by each column in a row to find the X values per column
                for (int row = 0; row < xLength; row++)
                {
                    for (int col = 0; col < xLength; col++)
                    {
                        grid[row, col] = input[col];
                    }
                }
            }
            else if (type == 1) // if y coordinate
            {
                int yLength = input.Length; // go through by each row in a column to find the Y values per row
                for (int col = 0; col < yLength; col++)
                {
                    for (int row = 0; row < yLength; row++)
                    {
                        grid[col, row] = input[col];
                    }
                }
            }
            return grid;
        }

        /// <summary>
        /// finds positions on a linearlly spaced meshgrid to interpolate points onto
        /// </summary>
        /// <param name="range">range of area</param>
        /// <param name="theta">possible angles</param>
        /// <returns>a struct of possible positions</returns>
        private posStruct[] FindPos(double[] range, double[] theta)
        {
            posStruct[] outStruct = new posStruct[(range.Length) * (theta.Length)]; // retued output struct of positions
            posStruct outVal = new posStruct();
            int index = 0;
            for (int col = 0; col < theta.Length; col++)
            {
                if (col == 0)   // first row column of the grid (will be 0,0)
                {
                    outVal.X = 0; outVal.Y = 0;
                    outStruct[index] = outVal;
                    index += 1;
                }
                for (int row = 1; row < range.Length; row++)    // find the XY values of the calculated position 
                {

                    outVal.X = range[row] * (Math.Sin(theta[col] * (Math.PI / 180)));
                    outVal.Y = range[row] * (Math.Cos(theta[col] * (Math.PI / 180)));
                    outStruct[index] = outVal;
                    index += 1;
                }
            }
            return outStruct;
        }

        // variables that are used to init values for the first use, reduce processing time on multiple iterations
        private static bool isInterpolateInit = false;
        private static Delaunator interpoalteDel;

        InterpolationData[] structArray = new InterpolationData[16066];
        static int[] zi;

        /// <summary>
        /// interpolates data onto a position array using a delayney triangulation
        /// </summary>
        /// <param name="positions">location positions on grid</param>
        /// <param name="v">found strenghs of points</param>
        /// <returns>a struct of interpolate data onto the positions</returns>
        private InterpolationData[] InterpolateData(posStruct[] positions, double[,] v)
        {
            // find the sum of the first row in the dataset
            double sum = 0;
            for (int colCount = 0; colCount < 63; colCount++)
                sum += v[0, colCount];

            // first value of the struct array will always have this value calculation
            structArray[0] = new InterpolationData(positions[0].X, positions[0].Y, sum * 0.01587301587); // 0.01587301587 is 1/63

            int pointCount = 1;

            // goes through all positions on a row column basis to interpolate the strengths to the meshgrid
            for (int col = 0; col < v.GetLength(1); col++)
            {
                if (Math.Abs(positions[pointCount].X) > 2 )
                {
                    pointCount += 1;
                    continue;
                }
                // correlate each of the interpolated strengths onto the XY meshgrid found earlier
                for (int row = 1; row < v.GetLength(0); row++)
                {
                    structArray[pointCount] = new InterpolationData(positions[pointCount].X, positions[pointCount].Y, v[row, col]);
                    pointCount += 1;

                }  
            }
            // initial creation of the delauney triangle, does not change thus only needs to be created once
            if (!isInterpolateInit)
            {
                // list of test points used for the delauney trianglulations
                ObservableCollection<IPoint> testPoints = new ObservableCollection<IPoint>();

                Point dummy = new Point();
                // for points in struct array (256x63 matrix)
                testPoints.Clear();
                for (int i = 0; i < structArray.Length; i++)
                {
                    dummy.X = structArray[i].X;
                    dummy.Y = structArray[i].Y;
                    testPoints.Add(dummy);
                }

                interpoalteDel = new Delaunator(testPoints);   // create delauney trianges 256x63

                // search within each of the triangles to find the interpolated poitns within the triangles to base bias change from
                zi = TSearch(interpoalteDel, structArray, points);
                // make sure this doesn't run after the first time, is static
                isInterpolateInit = true;
            }
            return GridData_From_Cache(zi, structArray, points);
        }

        // values that will never change in size or value, to help speed up the processing time of the program
        static bool tSearchInit = false;
        int[] values = new int[120000];
        readonly double eps = 1 * Math.Pow(10, -12);
        readonly int nelem = 32044;
        readonly int np = 40000;

        double[] minX = new double[32044]; double[] maxX = new double[32044];
        double[] minY = new double[32044]; double[] maxY = new double[32044];



        /// <summary>
        /// searches within triangles to see if points are within those delauney triangles
        /// </summary>
        /// <param name="dt">delauney triangle to compare to</param>
        /// <param name="points">points to see if within triangles</param>
        /// <param name="xiyi">coordinate locations of points</param>
        /// <returns>index of the points within triangles</returns>
        private int[] TSearch(Delaunator dt, InterpolationData[] points, ObservableCollection<IPoint> xiyi)
        {
            // initial values that need to be created only on the first run, are static
            if (!tSearchInit)
            {
                // search in point groupings of 3 (single triangle) and find the minimum and maximum XY values of the triangle sets
                var idx = 0;
                for (var i = 0; i < nelem; i++, idx += 3)
                {
                    minX[i] = Min3(points[dt.Triangles[idx]].X, points[dt.Triangles[idx + 1]].X, points[dt.Triangles[idx + 2]].X) - eps;
                    maxX[i] = Max3(points[dt.Triangles[idx]].X, points[dt.Triangles[idx + 1]].X, points[dt.Triangles[idx + 2]].X) + eps;
                    minY[i] = Min3(points[dt.Triangles[idx]].Y, points[dt.Triangles[idx + 1]].Y, points[dt.Triangles[idx + 2]].Y) - eps;
                    maxY[i] = Max3(points[dt.Triangles[idx]].Y, points[dt.Triangles[idx + 1]].Y, points[dt.Triangles[idx + 2]].Y) + eps;
                }
                // make sure this doesnt search again after the first time running
                tSearchInit = true;
            }
            int counter = 0;

            // values for linear searching within the triangles, x0 y0 are initial values , a11 through a22 are a square matrix for linear calculations, det is determinate
            double x0, y0, a11, a12, a21, a22, det;
            x0 = y0 = 0.0;
            a11 = a12 = a21 = a22 = 0.0;
            det = 0.0;
            int k = nelem;  // k is counter of elements

            // go thourgh iterative values to check whether the xiyi pairing are within the desired locations
            for (int kp = 0; kp < np; kp++)
            {
                double xt = xiyi[kp].X;
                double yt = xiyi[kp].Y;
                // removes unwanted points (out of range from both left and right side)
                if (xt < -5 || xt > 5 || yt < 0.5 || yt > 10.5)
                {
                    for (int i = counter; i < counter + 3; i++)
                    {
                        values[i] = -1;
                    }
                    counter += 3;

                    continue;
                }
                // check if last triangle contains the next point
                if (k < nelem)
                {
                    // find change in distance and perform cramer's rule to find the values of each of the points
                    var dx1 = xt - x0;
                    var dx2 = yt - y0;
                    var c1 = (a22 * dx1 - a21 * dx2) / det;
                    var c2 = (-a12 * dx1 + a11 * dx2) / det;
                    if (c1 >= -eps && c2 >= -eps && (c1 + c2) <= (1 + eps))
                    {
                        // if this value is go through counter to find the valid triangulation values to see if the previous triangle contains the next point
                        for (int i = counter; i < counter + 3; i++)
                        {
                            values[i] = dt.Triangles[3 * k + (i - counter)];
                        }
                        counter += 3;

                        continue;
                    }
                }
                // it doesn't, so go thru all elements
                for (k = 0; k < nelem; k++)
                {
                    if (xt >= minX[k] && xt <= maxX[k] && yt >= minY[k] && yt <= maxY[k])
                    {
                        // element inside the minimum rectangle, examine closesly
                        x0 = points[dt.Triangles[3 * k]].X;
                        y0 = points[dt.Triangles[3 * k]].Y;
                        a11 = points[dt.Triangles[3 * k + 1]].X - x0;
                        a12 = points[dt.Triangles[3 * k + 1]].Y - y0;
                        a21 = points[dt.Triangles[3 * k + 2]].X - x0;
                        a22 = points[dt.Triangles[3 * k + 2]].Y - y0;
                        det = a11 * a22 - a21 * a12;
                        // solve system
                        var dx1 = xt - x0;
                        var dx2 = yt - y0;
                        var c1 = (a22 * dx1 - a21 * dx2) / det;
                        var c2 = (-a12 * dx1 + a11 * dx2) / det;
                        if ((c1 >= -eps) && (c2 >= -eps) && ((c1 + c2) <= (1 + eps)))
                        {
                            for (int i = counter; i < counter + 3; i++)
                            {
                                values[i] = dt.Triangles[3 * k + (i - counter)];
                            }
                            counter += 3;

                            break;
                        }
                    } // endif # examine this element closely
                } // endfor # each element
                if (k == nelem)
                {
                    for (int i = counter; i < counter + 3; i++)
                    {
                        values[i] = -1;
                    }
                    counter += 3;
                }
            }
            return values;
        }

        /// <summary>
        /// finalizes interpolation by adjusting the points onto the grid
        /// </summary>
        /// <param name="zi">array of indexes of points within triangles</param>
        /// <param name="points">points to interpolate</param>
        /// <param name="xiyi">xy coordinates of final grid</param>
        /// <returns></returns>        
        InterpolationData[] finalStruct = new InterpolationData[40000];
        private InterpolationData[] GridData_From_Cache(int[] zi, InterpolationData[] points, ObservableCollection<IPoint> xiyi)
        {
            int counter = 0;
            int idx = 0;
            for (int i = 0; i < zi.Length; i += 3)
            {
                // i points are 3 vertices of triangle
                if (zi[i] == -1)
                {
                    finalStruct[counter] = new InterpolationData(xiyi[counter].X, xiyi[counter].Y, double.NaN);
                }
                else
                {
                    // get determinates of the three points of the triangle and find their magnitues
                    double x1 = points[zi[i]].X; double x2 = points[zi[i + 1]].X; double x3 = points[zi[i + 2]].X;
                    double y1 = points[zi[i]].Y; double y2 = points[zi[i + 1]].Y; double y3 = points[zi[i + 2]].Y;
                    double z1 = points[zi[i]].Strength; double z2 = points[zi[i + 1]].Strength; double z3 = points[zi[i + 2]].Strength;
                    double u1 = x2 - x1; double u2 = y2 - y1; double u3 = z2 - z1;
                    double v1 = x3 - x1; double v2 = y3 - y1; double v3 = z3 - z1;
                    double n1 = u2 * v3 - u3 * v2;
                    double n2 = u3 * v1 - u1 * v3;
                    double n3 = u1 * v2 - u2 * v1;
                    double mag = Math.Sqrt(n1 * n1 + n2 * n2 + n3 * n3);
                    n1 = n1 / mag; n2 = n2 / mag; n3 = n3 / mag;
                    // T is the final strength value found within the interpolated triangle, will be saved if strength is greater than 1500, else it will be a NaN, low strength
                    double D = -(n1 * x1 + n2 * y1 + n3 * z1);
                    double T = -(n1 * xiyi[idx].X + n2 * xiyi[idx].Y + D) / n3;
                    if (T < 1500)
                    {
                        finalStruct[counter] = new InterpolationData(xiyi[counter].X, xiyi[counter].Y, double.NaN);
                    }
                    else
                    {
                        finalStruct[counter] = new InterpolationData(xiyi[counter].X, xiyi[counter].Y, T);
                    }
                }
                idx += 1;
                ++counter;
            }
            return finalStruct;
        }

        /// <summary>
        /// finds the max of 3 inputs
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double Max3(double a, double b, double c)
        {
            if (a < b) return (b < c ? c : b);
            else return (a < c ? c : a);
        }


        /// <summary>
        /// finds the minimum of 3 inputs
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double Min3(double a, double b, double c)
        {
            if (a > b) return (b > c ? c : b);
            else return (a > c ? c : a);
        }

        #endregion
    }
}